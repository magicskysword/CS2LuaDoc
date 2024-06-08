using Cysharp.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using NLog;

namespace CS2LuaDoc.Core;

public class SolutionLoader
{
    public SolutionLoader(string slnPath)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
        
        SlnPath = slnPath;
    }

    public static Logger Logger = LogManager.GetCurrentClassLogger();
    public bool IsLoaded { get; set; }
    public bool IsLoadSuccess { get; set; }
    public string SlnPath { get; }
    public MSBuildWorkspace? LoadWorkspace { get; set; }
    public Solution? LoadSolution { get; set; }
    public List<Compilation> CachedCompilations { get; set; } = new();
    // Reference:
    public List<MetadataReference> CachedMetadataReferences { get; set; } = new();
    public List<string> ExcludeAssembly { get; set; } = new();
    public List<string> ExcludeAssemblyPrefix { get; set; } = new();
    public List<string> ExcludeAssemblySuffix { get; set; } = new();
    public List<string> IncludeAssembly { get; set; } = new();

    public async UniTask LoadAsync()
    {
        if (IsLoaded) 
            return;
        
        LoadWorkspace = MSBuildWorkspace.Create();
        LoadSolution = await LoadWorkspace.OpenSolutionAsync(SlnPath);
        IsLoaded = true;
        
        if (LoadSolution != null)
        {
            foreach (var project in LoadSolution.Projects)
            {
                if(ExcludeAssembly.Contains(project.Name))
                    continue;
                
                if(ExcludeAssemblyPrefix.Any(prefix => project.Name.StartsWith(prefix)))
                    continue;
                
                if(ExcludeAssemblySuffix.Any(suffix => project.Name.EndsWith(suffix)))
                    continue;
                
                if(IncludeAssembly.Count > 0 && !IncludeAssembly.Contains(project.Name))
                    continue;
                
                var compilation = await project.GetCompilationAsync(CancellationToken.None);
                if (compilation != null) 
                    CachedCompilations.Add(compilation);
            }
            
            foreach (var compilation in CachedCompilations)
            {
                foreach (var reference in compilation.References)
                {
                    if (reference is PortableExecutableReference portableExecutableReference)
                    {
                        CachedMetadataReferences.Add(portableExecutableReference);
                    }
                }
            }
            
            IsLoadSuccess = true;
        }
        
        // 去重
        Func<MetadataReference, string> comparer = reference =>
        {
            if (reference is PortableExecutableReference portableExecutableReference)
            {
                return portableExecutableReference.Display ?? string.Empty;
            }
            return string.Empty;
        };
        CachedMetadataReferences = CachedMetadataReferences.DistinctBy(comparer).OrderBy(reference => reference.Display).ToList();

        foreach (var reference in CachedMetadataReferences)
        {
            Logger.Debug("Load Compliation {Compliation}", 
                reference.Display);
        }
    }
}