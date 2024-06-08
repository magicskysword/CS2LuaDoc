﻿using System.Text;
using System.Xml;
using Cysharp.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NLog;

namespace CS2LuaDoc.Core;

public class LuaAnnotationGenerator
{
    public int PreClassInOneFileCount { get; set; } = 150;
    public List<LuaAnnotationClassFilter> ClassFilters { get; set; } = new();
    public ProjectMetaData ProjectMetaData { get; set; } = null!;
    public string OutputPath { get; set; } = string.Empty;
    
    private static Logger logger = LogManager.GetCurrentClassLogger();
    
    public async UniTask GenerateAsync()
    {
        await GenerateMeta(OutputPath);
        
        var allClassNamespace = ProjectMetaData.ClassMetaDataCollection
            .Select(x => x.Value)
            .Where(x => ClassFilters.All(filter => filter.FilterClass(x)))
            .GroupBy(x => x.Namespace)
            .ToList();
        
        for (var index = 0; index < allClassNamespace.Count; index++)
        {
            var group = allClassNamespace[index];
            var namespaceName = group.Key;
            
            logger.Debug("GenerateNamespace {Namespace} {Index}/{Max}", namespaceName, index + 1, allClassNamespace.Count);
            
            var classMetaDataList = group.ToList();
            await GenerateNamespace(namespaceName, classMetaDataList);
        }
    }

    private async UniTask GenerateNamespace(string? namespaceName, List<ClassMetaData> classMetaDataList)
    {
        var classCounter = 0;
        var classGroup = classMetaDataList.GroupBy(x => classCounter++ / PreClassInOneFileCount).ToList();
        for (var index = 0; index < classGroup.Count; index++)
        {
            var group = classGroup[index];
            var classMetaDataListInFile = group.ToList();
            if(classGroup.Count == 1)
            {
                await GenerateFile(-1, namespaceName, classMetaDataListInFile);
            }
            else
            {
                await GenerateFile(index, namespaceName, classMetaDataListInFile);
            }
        }
    }

    private async UniTask GenerateMeta(string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---@meta");
        sb.AppendLine($"-- Generated by CS2LuaDoc");
        sb.AppendLine($"-- Version: {typeof(LuaAnnotationGenerator).Assembly.GetName().Version}");
        sb.AppendLine($"-- Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("CS = {}");
        sb.AppendLine();
        await File.WriteAllTextAsync(Path.Combine(outputPath, ".meta.lua"), sb.ToString());
    }
    
    private async UniTask GenerateFile(int index, string? namespaceName, List<ClassMetaData> classMetaDataListInFile)
    {
        string fileName;
        string namespaceNameInFile;
        if (string.IsNullOrEmpty(namespaceName))
        {
            namespaceNameInFile = "CS_Global";
        }
        else
        {
            namespaceNameInFile = namespaceName;
        }
        
        if (index >= 0)
        {
            fileName = $"{namespaceNameInFile}_{index}.lua";
        }
        else
        {
            fileName = $"{namespaceNameInFile}.lua";
        }

        
        var sb = new StringBuilder();
        sb.AppendLine("---@meta");
        sb.AppendLine($"-- namespace {namespaceName}");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(namespaceName))
        {
            var lastNamespaceName = namespaceName.Split('.').Last();
            
            sb.AppendLine($"local {lastNamespaceName} = {{}}");
            sb.AppendLine($"CS.{namespaceName} = {lastNamespaceName}");
        }
        
        sb.AppendLine();
        foreach (var classMetaData in classMetaDataListInFile)
        {
            GenerateClass(namespaceName, classMetaData, sb);
        }
        
        await File.WriteAllTextAsync(Path.Combine(OutputPath, fileName), sb.ToString());
    }

    private void GenerateClass(string? namespaceName, ClassMetaData classMetaData, StringBuilder sb)
    {
        if (!classMetaData.IsPublic)
        {
            return;
        }
        
        if(TryAnalysisNormalRemark(classMetaData.RawRemark, out var remark))
        {
            foreach (var remarkOneLine in remark.Split('\n'))
            {
                sb.AppendLine($"-- {remarkOneLine}");
            }
        }
        sb.Append($"---@class {classMetaData.GetFullName()}");
        // 继承处理
        if (classMetaData.BaseClassMetaData != null)
        {
            sb.Append($" : {classMetaData.BaseClassMetaData.GetFullName()}");
        }
        sb.AppendLine();
        
        // 泛型参数处理
        if (classMetaData.IsGenericClass)
        {
            foreach (var typeParameter in classMetaData.GenericTypeParameters)
            {
                sb.Append($"---@generic {typeParameter.Name}");
                if (typeParameter.ConstraintTypes.Count > 0)
                {
                    sb.Append(" : ");
                }
                for (var index = 0; index < typeParameter.ConstraintTypes.Count; index++)
                {
                    var constraintType = typeParameter.ConstraintTypes[index];
                    sb.Append($"{GetLuaType(constraintType)}");
                    if (index != typeParameter.ConstraintTypes.Count - 1)
                    {
                        sb.Append(" | ");
                    }
                }

                sb.AppendLine();
            }
        }
        
        foreach (var memberMetaData in classMetaData.FieldMetaDataList)
        {
            GenerateField(classMetaData, memberMetaData, sb);
        }
        foreach (var memberMetaData in classMetaData.PropertyMetaDataList)
        {
            GenerateProperty(classMetaData, memberMetaData, sb);
        }
        foreach (var memberMetaData in classMetaData.EventMetaDataList)
        {
            GenerateEvent(classMetaData, memberMetaData, sb);
        }
        foreach (var methodMetaData in classMetaData.ConstructorMetaDataList)
        {
            GenerateCtor(classMetaData, methodMetaData, sb);
        }
        
        sb.AppendLine($"local {classMetaData.Name} = {{}}");
        
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"CS.{namespaceName}.{classMetaData.Name} = {classMetaData.Name}");
        }
        else
        {
            sb.AppendLine($"CS.{classMetaData.Name} = {classMetaData.Name}");
        }
        
        foreach (var memberMetaData in classMetaData.MethodMetaDataList)
        {
            GenerateMethod(classMetaData, memberMetaData, sb);
        }
        
        sb.AppendLine();
    }

    private void GenerateField(ClassMetaData classMetaData, FieldMetaData fieldMetaData, StringBuilder sb)
    {
        if (!fieldMetaData.IsPublic)
        {
            return;
        }
        
        sb.Append($"---@field {fieldMetaData.Name} {GetLuaType(fieldMetaData.Type)}");
        if(TryAnalysisNormalRemark(fieldMetaData.RawRemark, out var remark))
        {
            var remarkInOneLine = remark.TransToSingleLine(false);
            sb.Append($" {remarkInOneLine}");
        }
        sb.AppendLine();
    }
    
    private void GenerateProperty(ClassMetaData classMetaData, PropertyMetaData propertyMetaData, StringBuilder sb)
    {
        if (!propertyMetaData.IsPublic)
        {
            return;
        }
        
        sb.Append($"---@field {propertyMetaData.Name} {GetLuaType(propertyMetaData.Type)}");
        if(TryAnalysisNormalRemark(propertyMetaData.RawRemark, out var remark))
        {
            var remarkInOneLine = remark.TransToSingleLine(false);
            sb.Append($" {remarkInOneLine}");
        }
        sb.AppendLine();
    }
    
    private void GenerateEvent(ClassMetaData classMetaData, EventMetaData eventMetaData, StringBuilder sb)
    {
        if (!eventMetaData.IsPublic)
        {
            return;
        }
        
        sb.Append($"---@field {eventMetaData.Name} {GetLuaType(eventMetaData.Type)}");
        if(TryAnalysisNormalRemark(eventMetaData.RawRemark, out var remark))
        {
            var remarkInOneLine = remark.TransToSingleLine(false);
            sb.Append($" {remarkInOneLine}");
        }
        sb.AppendLine();
    }
    
    private void GenerateCtor(ClassMetaData classMetaData, MethodMetaData methodMetaData, StringBuilder sb)
    {
        if (!methodMetaData.IsPublic)
        {
            return;
        }
        
        if (methodMetaData.Name != ".ctor") return;
        
        sb.Append($"---@overload fun(");
        var parameters = methodMetaData.Parameters;
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var isOptional = parameter.IsRefOrOut || parameter.IsParams;
            sb.Append(parameter.Name);
            if (isOptional)
            {
                sb.Append("?");
            }
            if (i != parameters.Count - 1)
            {
                sb.Append(", ");
            }
        }
        sb.Append($"): {GetLuaType(classMetaData.TypeSymbol)}");
        sb.AppendLine();
    }
    
    private void GenerateMethod(ClassMetaData classMetaData, MethodMetaData methodMetaData, StringBuilder sb)
    {
        if (!methodMetaData.IsPublic)
        {
            return;
        }
        
        var parameters = methodMetaData.Parameters;
        
        TryAnalysisMethodRemark(methodMetaData.RawRemark, out var remarkDoc);
        
        if (remarkDoc?.TryVisitSummary(out var summary) == true && !string.IsNullOrEmpty(summary))
        {
            foreach (var summaryOneLine in summary.Split('\n'))
            {
                sb.AppendLine($"-- {summaryOneLine}");
            }
        }
        
        // 泛型参数处理
        if (methodMetaData.IsGenericMethod)
        {
            foreach (var typeParameter in methodMetaData.TypeParameters.Concat(classMetaData.GenericTypeParameters))
            {
                sb.Append($"---@generic {typeParameter.Name}");
                if (typeParameter.ConstraintTypes.Count > 0)
                {
                    sb.Append(" : ");
                }
                for (var index = 0; index < typeParameter.ConstraintTypes.Count; index++)
                {
                    var constraintType = typeParameter.ConstraintTypes[index];
                    sb.Append($"{GetLuaType(constraintType)}");
                    if (index != typeParameter.ConstraintTypes.Count - 1)
                    {
                        sb.Append(" | ");
                    }
                }

                sb.AppendLine();
            }
        }
        
        // 参数处理
        foreach (var parameterMetaData in methodMetaData.Parameters)
        {
            var paramRemark = string.Empty;
            var paramName = ParseParameterName(parameterMetaData.Name);
            if (remarkDoc?.TryVisitParam(paramName, out var txt) == true)
            {
                paramRemark = txt.TransToSingleLine();
            }
            
            sb.Append($"---@param {paramName} {GetLuaType(parameterMetaData.Type)}");

            if (parameterMetaData.IsRef)
            {
                sb.Append(" ref:");
            }
            else if (parameterMetaData.IsOut)
            {
                sb.Append(" out:");
            }
            else if (parameterMetaData.IsParams)
            {
                sb.Append(" params:");
            }
            
            if (!string.IsNullOrEmpty(paramRemark))
            {
                sb.Append(' ');
                sb.Append(paramRemark);
            }
            
            sb.AppendLine();
        }
        
        // 返回值处理
        if (methodMetaData.ReturnType.SpecialType != SpecialType.System_Void)
        {
            sb.Append($"---@return {GetLuaType(methodMetaData.ReturnType)}");
            foreach (var parameter in methodMetaData.Parameters)
            {
                if (parameter.IsRefOrOut)
                {
                    sb.Append($", {GetLuaType(parameter.Type)}");
                }
            }
            if (remarkDoc?.TryVisitReturns(out var returnRemark) == true)
            {
                returnRemark = returnRemark.TransToSingleLine();
                sb.Append($" {returnRemark}");
            }
            sb.AppendLine();
        }
        
        // 方法定义
        var methodName = methodMetaData.Name;
        if (methodMetaData.IsStatic)
        {
            sb.Append($"function {classMetaData.Name}.{methodName}(");
        }
        else
        {
            sb.Append($"function {classMetaData.Name}:{methodName}(");
        }
        
        int normalParamCount = 0;
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            if (parameter.IsOut)
            {
                continue;
            }
            
            if (normalParamCount != 0)
            {
                sb.Append(", ");
            }
            
            normalParamCount++;
            sb.Append(ParseParameterName(parameter.Name));
        }
        sb.AppendLine(") end");
        
        sb.AppendLine();
    }

    /// <summary>
    /// 把Lua的关键词转换成带下划线的
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private string ParseParameterName(string name)
    {
        return name switch
        {
            "function" => "_function",
            "end" => "_end",
            "local" => "_local",
            "repeat" => "_repeat",
            "if" => "_if",
            "else" => "_else",
            "elseif" => "_elseif",
            _ => name
        };
    }


    private string GetLuaType(ITypeSymbol type)
    {
        string retTypeStr;

        switch (type)
        {
            // 委托类型处理
            case INamedTypeSymbol namedTypeSymbol when namedTypeSymbol.DelegateInvokeMethod != null:
            {
                var delegateInvokeMethod = namedTypeSymbol.DelegateInvokeMethod;
                var sb = new StringBuilder();
                sb.Append("fun(");
                var parameters = delegateInvokeMethod.Parameters;
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var isOptional = parameter.IsOptional;
                    sb.Append(parameter.Name);
                    sb.Append(": ");
                    sb.Append(GetLuaType(parameter.Type));
                    if (isOptional)
                    {
                        sb.Append("?");
                    }
                    if (i != parameters.Length - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append(")");
                if(delegateInvokeMethod.ReturnType.SpecialType != SpecialType.System_Void)
                {
                    sb.Append(" : ");
                    sb.Append(GetLuaType(delegateInvokeMethod.ReturnType));
                }
                retTypeStr = sb.ToString();
                break;
            }
            // 数组类型处理
            case IArrayTypeSymbol arrayTypeSymbol:
            {
                var sb = new StringBuilder();
                sb.Append(GetLuaType(arrayTypeSymbol.ElementType));
                sb.Append("[]");
                retTypeStr = sb.ToString();
                break;
            }
            // 列表类型处理
            case INamedTypeSymbol namedTypeSymbol when namedTypeSymbol.IsGenericType && namedTypeSymbol.Name == "List":
            {
                var sb = new StringBuilder();
                sb.Append(GetLuaType(namedTypeSymbol.TypeArguments[0]));
                sb.Append("[]");
                retTypeStr = sb.ToString();
                break;
            }
            
            default:
            {
                switch (type.SpecialType)
                {
                    // 基本类型处理
                    // object处理成any
                    case SpecialType.System_Object:
                        retTypeStr = "any";
                        break;
                    // string处理成string
                    case SpecialType.System_String:
                        retTypeStr = "string";
                        break;
                    // bool处理成boolean
                    case SpecialType.System_Boolean:
                        retTypeStr = "boolean";
                        break;
                    // 数值类型处理
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                        retTypeStr = $"number | {type.GetFullName()}";
                        break;
                    default:
                        retTypeStr = type.GetFullName();
                        break;
                }
                break;
            }
        }

        return retTypeStr;
    }
    
    private bool TryAnalysisNormalRemark(string rawRemark, out string txt)
    {
        if(string.IsNullOrEmpty(rawRemark))
        {
            txt = string.Empty;
            return false;
        }
        
        var remark = rawRemark.Trim();
        var xmlDocument = remark.TryParseXmlDoc();
        if (xmlDocument == null)
        {
            txt = string.Empty;
            return false;
        }
        
        return xmlDocument.TryVisitSummary(out txt);
    }
    
    private bool TryAnalysisMethodRemark(string rawRemark, out XmlDocument? o)
    {
        if(string.IsNullOrEmpty(rawRemark))
        {
            o = null;
            return false;
        }
        
        var remark = rawRemark.Trim();
        var xmlDocument = remark.TryParseXmlDoc();
        if (xmlDocument == null)
        {
            o = null;
            return false;
        }
        
        o = xmlDocument;
        return true;
    }
}