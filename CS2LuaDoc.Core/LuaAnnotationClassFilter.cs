namespace CS2LuaDoc.Core;

public abstract class LuaAnnotationClassFilter
{
    /// <summary>
    /// return true if the class should be included
    /// </summary>
    /// <param name="classMetaData"></param>
    /// <returns></returns>
    public abstract bool FilterClass(ClassMetaData classMetaData);
}

public class LuaAnnotationClassNamespaceIncludeFilter : LuaAnnotationClassFilter
{
    private string Namespace { get; set; } = string.Empty;

    public LuaAnnotationClassNamespaceIncludeFilter SetNamespace(string _namespace)
    {
        if(!_namespace.EndsWith(".") && !string.IsNullOrEmpty(_namespace))
            _namespace += ".";
        
        Namespace = _namespace;
        return this;
    }

    public override bool FilterClass(ClassMetaData classMetaData)
    {
        return classMetaData.GetFullName().StartsWith(Namespace);
    }
    
    public static IEnumerable<LuaAnnotationClassNamespaceIncludeFilter> CreateIncludeFilters(IEnumerable<string> namespaces)
    {
        return namespaces.Select(ns => new LuaAnnotationClassNamespaceIncludeFilter().SetNamespace(ns));
    }
}

public class LuaAnnotationClassNamespaceExcludeFilter : LuaAnnotationClassFilter
{
    private string Namespace { get; set; } = string.Empty;

    public LuaAnnotationClassNamespaceExcludeFilter SetNamespace(string _namespace)
    {
        if(!_namespace.EndsWith(".") && !string.IsNullOrEmpty(_namespace))
            _namespace += ".";
        
        Namespace = _namespace;
        return this;
    }

    public override bool FilterClass(ClassMetaData classMetaData)
    {
        return !classMetaData.GetFullName().StartsWith(Namespace);
    }
    
    public static IEnumerable<LuaAnnotationClassNamespaceExcludeFilter> CreateExcludeFilters(IEnumerable<string> namespaces)
    {
        return namespaces.Select(ns => new LuaAnnotationClassNamespaceExcludeFilter().SetNamespace(ns));
    }
}

public class LuaAnnotationClassPublicFilter : LuaAnnotationClassFilter
{
    public override bool FilterClass(ClassMetaData classMetaData)
    {
        return classMetaData.IsPublic;
    }
}

public class LuaAnnotationClassNameFilter : LuaAnnotationClassFilter
{
    public string Name { get; private set; } = string.Empty;

    public LuaAnnotationClassNameFilter SetName(string name)
    {
        Name = name;
        return this;
    }

    public override bool FilterClass(ClassMetaData classMetaData)
    {
        return classMetaData.Name == Name;
    }
}