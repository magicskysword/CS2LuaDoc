using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace CS2LuaDoc.Core;

public class ProjectMetaData
{
    public Dictionary<string, ClassMetaData> ClassMetaDataCollection { get; set; } = new(); 
}

public abstract class BaseMetaData
{
    public string Name { get; set; } = string.Empty;
    public string RawRemark { get; set; } = string.Empty;
}

public class ClassMetaData : BaseMetaData
{
    public bool IsPublic { get; set; } = true;
    public bool IsGenericClass { get; set; }
    public string? Namespace { get; set; } = null;
    public List<MethodMetaData> ConstructorMetaDataList { get; set; } = new();
    public List<FieldMetaData> FieldMetaDataList { get; set; } = new();
    public List<PropertyMetaData> PropertyMetaDataList { get; set; } = new();
    public List<MethodMetaData> MethodMetaDataList { get; set; } = new();
    public List<EventMetaData> EventMetaDataList { get; set; } = new();
    public List<TypeParameterMetaData> GenericTypeParameters { get; set; } = new();
    public ClassMetaData? BaseClassMetaData { get; set; }
    public ITypeSymbol TypeSymbol { get; set; } = null!;
    public List<ITypeSymbol> GenericTypeArguments { get; set; } = new();

    public string GetFullName(Func<ITypeSymbol, string>? genericTypeNameGetter = null)
    {
        if (!string.IsNullOrEmpty(Namespace))
        {
            return $"{Namespace}.{GetTypeName(genericTypeNameGetter)}";
        }
        
        return GetTypeName(genericTypeNameGetter);
    }

    public string GetTypeName(Func<ITypeSymbol, string>? genericTypeNameGetter = null)
    {
        genericTypeNameGetter ??= t => t.GetFullName();
        
        if (IsGenericClass)
        {
            try
            {
                var fullName = new StringBuilder();
                fullName.Append(TypeSymbol.Name);
                fullName.Append("<");
                fullName.Append(string.Join(", ", GenericTypeArguments.Select(genericTypeNameGetter)));
                fullName.Append(">");
                return fullName.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(Name);
                Console.WriteLine(TypeSymbol.GetFullName());
                throw;
            }
        }

        return Name;
    }
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
    public bool IsIndexer { get; set; } = false;
    public bool HasGetter { get; set; } = false;
    public bool HasSetter { get; set; } = false;
    public List<ParameterMetaData> Parameters { get; set; } = new();
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
    public List<ParameterMetaData> Parameters { get; set; } = new();
    public bool IsGenericMethod { get; set; }
    /// <summary>
    /// 泛型参数
    /// </summary>
    public List<TypeParameterMetaData> TypeParameters { get; set; } = new();

    public MethodMetaData GetExtensionMethod()
    {
        var method = new MethodMetaData
        {
            Name = Name,
            IsStatic = false,
            IsPublic = IsPublic,
            ReturnType = ReturnType,
            Parameters = Parameters.Skip(1).ToList(),
            IsGenericMethod = IsGenericMethod,
            TypeParameters = TypeParameters
        };

        return method;
    }
}

public class ParameterMetaData : BaseMetaData
{
    public ITypeSymbol Type { get; set; } = null!;
    public bool IsRefOrOut => IsRef || IsOut;
    public bool IsRef { get; set; }
    public bool IsOut { get; set; }
    public bool IsParams { get; set; }
    public bool IsOptional { get; set; } = false;
}

public class TypeParameterMetaData : BaseMetaData
{
    public ITypeSymbol Type { get; set; } = null!;
    public List<ITypeSymbol> ConstraintTypes { get; set; } = new();
}