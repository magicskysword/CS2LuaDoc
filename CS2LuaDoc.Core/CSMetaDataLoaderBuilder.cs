namespace CS2LuaDoc.Core;

public class CSMetaDataLoaderBuilder
{
    private string solutionPath = string.Empty;
    private List<string> excludeAssembly = new();
    private List<string> excludeAssemblyPrefix = new();
    private List<string> excludeAssemblySuffix = new();
    private List<string> includeAssembly = new();
    
    public CSMetaDataLoaderBuilder()
    {
        
    }
    
    public CSMetaDataLoaderBuilder SetSolutionPath(string _solutionPath)
    {
        solutionPath = _solutionPath;
        return this;
    }
    
    public CSMetaDataLoaderBuilder SetExcludeAssembly(string[] assemblies)
    {
        excludeAssembly.Clear();
        excludeAssemblyPrefix.Clear();
        excludeAssemblySuffix.Clear();
        foreach (var assembly in assemblies)
        {
            if (assembly.StartsWith("prefix:"))
            {
                var realAssembly = assembly.Substring(7);
                excludeAssemblyPrefix.Add(realAssembly);
            }
            else if (assembly.StartsWith("suffix:"))
            {
                var realAssembly = assembly.Substring(7);
                excludeAssemblySuffix.Add(realAssembly);
            }
            else
            {
                excludeAssembly.Add(assembly);
            }
        }
        return this;
    }
    
    public CSMetaDataLoaderBuilder SetIncludeAssembly(string[] assemblies)
    {
        includeAssembly.Clear();
        foreach (var assembly in assemblies)
        {
            includeAssembly.Add(assembly);
        }
        return this;
    }
    
    public CSMetaDataLoader Build()
    {
        if (string.IsNullOrEmpty(solutionPath))
        {
            throw new Exception("solutionPath is null");
        }
        
        var solutionLoader = new SolutionLoader(solutionPath);
        solutionLoader.ExcludeAssembly.AddRange(excludeAssembly);
        solutionLoader.ExcludeAssemblyPrefix.AddRange(excludeAssemblyPrefix);
        solutionLoader.ExcludeAssemblySuffix.AddRange(excludeAssemblySuffix);
        solutionLoader.IncludeAssembly.AddRange(includeAssembly);
        return new CSMetaDataLoader(solutionLoader);
    }
}