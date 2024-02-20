using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CS2LuaDoc.Core;

public class ProjectMetaData
{
    public List<ClassMetaData> ClassMetaDataList { get; set; } = new(); 
}

public abstract class BaseMetaData
{
    public string Name { get; set; } = string.Empty;
    public string RawRemark { get; set; } = string.Empty;
}

public class ClassMetaData : BaseMetaData
{
    public bool IsPublic { get; set; } = true;
    public string? Namespace { get; set; } = null;
    public List<MethodMetaData> ConstructorMetaDataList { get; set; } = new();
    public List<FieldMetaData> FieldMetaDataList { get; set; } = new();
    public List<PropertyMetaData> PropertyMetaDataList { get; set; } = new();
    public List<MethodMetaData> MethodMetaDataList { get; set; } = new();
    public List<EventMetaData> EventMetaDataList { get; set; } = new();
}

public class FieldMetaData : BaseMetaData
{
    public ITypeSymbol Type { get; set; } = null!;
    public bool IsPublic { get; set; } = true;
}

public class PropertyMetaData : BaseMetaData
{
    public ITypeSymbol Type { get; set; } = null!;
    public bool IsPublic { get; set; } = true;
}

public class EventMetaData : BaseMetaData
{
    public ITypeSymbol Type { get; set; } = null!;
    public bool IsPublic { get; set; } = true;
}

public class MethodMetaData : BaseMetaData
{
    public bool IsStatic { get; set; }  
    public bool IsPublic { get; set; } = true;
    public ITypeSymbol ReturnType { get; set; } = null!;
    public List<MethodParameterMetaData> Parameters { get; set; } = new();
    public bool IsGenericMethod { get; set; }
    /// <summary>
    /// 泛型参数
    /// </summary>
    public List<MethodTypeParameterMetaData> TypeParameters { get; set; } = new();
}

public class MethodParameterMetaData : BaseMetaData
{
    public ITypeSymbol Type { get; set; } = null!;
    public bool IsRefOrOut => IsRef || IsOut;
    public bool IsRef { get; set; }
    public bool IsOut { get; set; }
    public bool IsParams { get; set; }
}

public class MethodTypeParameterMetaData : BaseMetaData
{
    public ITypeSymbol Type { get; set; } = null!;
    public List<ITypeSymbol> ConstraintTypes { get; set; } = new();
}