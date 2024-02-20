using Cysharp.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

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

    public bool IsLoaded { get; set; }
    public bool IsLoadSuccess { get; set; }
    public string SlnPath { get; }
    public MSBuildWorkspace? LoadWorkspace { get; set; }
    public Solution? LoadSolution { get; set; }
    public List<Compilation> CachedCompilations { get; set; } = new();
    // Reference:
    public List<MetadataReference> CachedMetadataReferences { get; set; } = new();
    
    public async UniTask LoadAsync()
    {
        if (IsLoaded) 
            return;
        
        LoadWorkspace = MSBuildWorkspace.Create();
        LoadSolution = await LoadWorkspace.OpenSolutionAsync(SlnPath);
        IsLoaded = true;
        
        if (LoadSolution != null)
        {
            IsLoadSuccess = true;
            foreach (var project in LoadSolution.Projects)
            {
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
        }
    }
}