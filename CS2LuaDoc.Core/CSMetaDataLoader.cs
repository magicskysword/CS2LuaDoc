﻿using Cysharp.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using NLog;

namespace CS2LuaDoc.Core;

public class CSMetaDataLoader
{
    public CSMetaDataLoader(SolutionLoader solutionLoader)
    {
        SolutionLoader = solutionLoader;
    }
    
    public SolutionLoader SolutionLoader { get; }
    
    private static Logger logger = LogManager.GetCurrentClassLogger();
    
    public async UniTask<ProjectMetaData> LoadAsync()
    {
        var projectMetaData = new ProjectMetaData();
        if (!SolutionLoader.IsLoaded)
        {
            await SolutionLoader.LoadAsync();
        }

        if (!SolutionLoader.IsLoadSuccess)
        {
            throw new Exception("工程加载失败");
        }

        for (var index = 0; index < SolutionLoader.CachedCompilations.Count; index++)
        {
            var compilation = SolutionLoader.CachedCompilations[index];
            logger.Debug("Load Compliation {Compliation} {Index}/{Max}", 
                compilation.AssemblyName, 
                index + 1, 
                SolutionLoader.CachedCompilations.Count);

            var visitor = new ClassDeclarationVisitor();
            visitor.Visit(compilation.GlobalNamespace);

            foreach (var classSymbol in visitor.classDeclarations)
            {
                TryLoadClass(projectMetaData, classSymbol);
            }
        }

        return projectMetaData;
    }
    
    private ClassMetaData? TryLoadClass(ProjectMetaData projectMetaData,INamedTypeSymbol classSymbol)
    {
        var classIdentifier = classSymbol.GetFullIdentify();
        // 排除重复的类
        if(projectMetaData.ClassMetaDataCollection.TryGetValue(classIdentifier, out var existClassMetaData))
            return existClassMetaData;
                
        if(classSymbol.IsImplicitClass)
            return null;
                
        if(classSymbol.IsAnonymousType)
            return null;

        if (classSymbol.IsUnboundGenericType)
            return null;
                
        if(classSymbol.Name.StartsWith("<"))
            return null;
                
        if(classSymbol.Name.Contains("="))
            return null;
                
        // 如果是委托类型，不处理
        if (classSymbol.BaseType != null && classSymbol.BaseType.Name == "MulticastDelegate")
            return null;
                
        var classMetaData = new ClassMetaData();
        projectMetaData.ClassMetaDataCollection[classIdentifier] = classMetaData;
        
        RejectClassMetaData(projectMetaData, classMetaData, classSymbol);
        return classMetaData;
    }

    private void RejectBaseMetaData(BaseMetaData metaData, ISymbol symbol)
    {
        metaData.Name = symbol.Name;
        var rawRemark = symbol.GetDocumentationCommentXml();
        metaData.RawRemark = rawRemark ?? string.Empty;
    }
    
    private void RejectClassMetaData(ProjectMetaData projectMetaData, ClassMetaData classMetaData,
        INamedTypeSymbol classSymbol)
    {
        RejectBaseMetaData(classMetaData, classSymbol);
        classMetaData.Namespace = classSymbol.GetFullNamespace();
        classMetaData.IsPublic = classSymbol.DeclaredAccessibility == Accessibility.Public;
        classMetaData.TypeSymbol = classSymbol;
        
        // 基类处理
        if (classSymbol.BaseType != null)
        {
            classMetaData.BaseClassMetaData = TryLoadClass(projectMetaData, classSymbol.BaseType);
        }
        
        // 构造函数
        classMetaData.ConstructorMetaDataList.AddRange(classSymbol.Constructors.Select(constructorSymbol =>
        {
            var constructorMetaData = new MethodMetaData();
            RejectMethodMetaData(projectMetaData, constructorMetaData, constructorSymbol);
            return constructorMetaData;
        }));
        
        // 泛型处理
        if (classSymbol.IsGenericType)
        {
            classMetaData.IsGenericClass = true;
            foreach (var typeParameterSymbol in classSymbol.TypeParameters)
            {
                var typeParameterMetaData = new TypeParameterMetaData();
                RejectTypeParameterMetaData(typeParameterMetaData, typeParameterSymbol);
                classMetaData.GenericTypeParameters.Add(typeParameterMetaData);
            }

            foreach (var typeArgument in classSymbol.TypeArguments)
            {
                classMetaData.GenericTypeArguments.Add(typeArgument);
            }
        }
        
        var members = classSymbol.GetMembers();
        foreach (var member in members)
        {
            switch (member.Kind)
            {
                case SymbolKind.Field:
                {
                    var fieldSymbol = (IFieldSymbol) member;
                    var fieldMetaData = new FieldMetaData();
                    RejectFieldMetaData(fieldMetaData, fieldSymbol);
                    classMetaData.FieldMetaDataList.Add(fieldMetaData);
                    break;
                }
                case SymbolKind.Event:
                {
                    var eventSymbol = (IEventSymbol) member;
                    var eventMetaData = new EventMetaData();
                    RejectEventMetaData(eventMetaData, eventSymbol);
                    classMetaData.EventMetaDataList.Add(eventMetaData);
                    break;
                }
                case SymbolKind.Property:
                {
                    var propertySymbol = (IPropertySymbol) member;
                    var propertyMetaData = new PropertyMetaData();
                    RejectPropertyMetaData(propertyMetaData, propertySymbol);
                    classMetaData.PropertyMetaDataList.Add(propertyMetaData);
                    break;
                }
                case SymbolKind.Method:
                {
                    var methodSymbol = (IMethodSymbol) member;
                    if(methodSymbol.Name.StartsWith("get_") || methodSymbol.Name.StartsWith("set_"))
                        continue;
                    
                    if(methodSymbol.Name == ".ctor")
                        continue;
                    
                    var methodMetaData = new MethodMetaData();
                    RejectMethodMetaData(projectMetaData, methodMetaData, methodSymbol);
                    classMetaData.MethodMetaDataList.Add(methodMetaData);
                    break;
                }
            }
        }
    }

    private void RejectEventMetaData(EventMetaData eventMetaData, IEventSymbol eventSymbol)
    {
        RejectBaseMetaData(eventMetaData, eventSymbol);
        eventMetaData.Type = eventSymbol.Type;
        eventMetaData.IsPublic = eventSymbol.DeclaredAccessibility == Accessibility.Public;
    }

    private void RejectFieldMetaData(FieldMetaData fieldMetaData, IFieldSymbol fieldSymbol)
    {
        RejectBaseMetaData(fieldMetaData, fieldSymbol);
        fieldMetaData.Type = fieldSymbol.Type;
        fieldMetaData.IsPublic = fieldSymbol.DeclaredAccessibility == Accessibility.Public;
    }
    
    private void RejectPropertyMetaData(PropertyMetaData propertyMetaData, IPropertySymbol propertySymbol)
    {
        RejectBaseMetaData(propertyMetaData, propertySymbol);
        propertyMetaData.Type = propertySymbol.Type;
        propertyMetaData.IsPublic = propertySymbol.DeclaredAccessibility == Accessibility.Public;
        // 如果是索引器
        if (propertySymbol.IsIndexer)
        {
            foreach (var parameter in propertySymbol.Parameters)
            {
                var parameterMetaData = new ParameterMetaData();
                RejectParameterMetaData(parameterMetaData, parameter);
                propertyMetaData.Parameters.Add(parameterMetaData);
            }
            propertyMetaData.IsIndexer = true;
        }
        // 是否有get和set
        propertyMetaData.HasGetter = propertySymbol.GetMethod != null;
        propertyMetaData.HasSetter = propertySymbol.SetMethod != null;
    }
    
    private void RejectMethodMetaData(ProjectMetaData projectMetaData, MethodMetaData methodMetaData,
        IMethodSymbol methodSymbol)
    {
        RejectBaseMetaData(methodMetaData, methodSymbol);
        methodMetaData.ReturnType = methodSymbol.ReturnType;
        methodMetaData.IsPublic = methodSymbol.DeclaredAccessibility == Accessibility.Public;
        methodMetaData.IsStatic = methodSymbol.IsStatic;
        IEnumerable<IParameterSymbol> parameters = methodSymbol.Parameters;
        
        foreach (var parameterSymbol in parameters)
        {
            var parameterMetaData = new ParameterMetaData();
            RejectParameterMetaData(parameterMetaData, parameterSymbol);
            methodMetaData.Parameters.Add(parameterMetaData);
        }
        
        if (methodSymbol.IsGenericMethod)
        {
            methodMetaData.IsGenericMethod = true;
            foreach (var typeParameterSymbol in methodSymbol.TypeParameters)
            {
                var typeParameterMetaData = new TypeParameterMetaData();
                RejectTypeParameterMetaData(typeParameterMetaData, typeParameterSymbol);
                methodMetaData.TypeParameters.Add(typeParameterMetaData);
            }
        }

        if (methodSymbol.IsExtensionMethod)
        {
            // 如果是扩展函数，注入到对应类型之中
            var targetType = methodSymbol.GetExtensionTypes();
            var extensionMethod = methodMetaData.GetExtensionMethod();
            foreach (var typeSymbol in targetType)
            {
                var classMetaData = TryLoadClass(projectMetaData, typeSymbol);
                if (classMetaData != null)
                {
                    classMetaData.MethodMetaDataList.Add(extensionMethod);
                }
            }
        }
    }

    private void RejectTypeParameterMetaData(TypeParameterMetaData typeParameterMetaData, ITypeParameterSymbol typeParameterSymbol)
    {
        RejectBaseMetaData(typeParameterMetaData, typeParameterSymbol);
        typeParameterMetaData.ConstraintTypes.AddRange(typeParameterSymbol.ConstraintTypes);
    }

    private void RejectParameterMetaData(ParameterMetaData parameterMetaData, IParameterSymbol parameterSymbol)
    {
        RejectBaseMetaData(parameterMetaData, parameterSymbol);
        parameterMetaData.Type = parameterSymbol.Type;
        parameterMetaData.IsRef = parameterSymbol.RefKind == RefKind.Ref;
        parameterMetaData.IsOut = parameterSymbol.RefKind == RefKind.Out;
        parameterMetaData.IsParams = parameterSymbol.IsParams;
        parameterMetaData.IsOptional = parameterSymbol.HasExplicitDefaultValue;
    }
}