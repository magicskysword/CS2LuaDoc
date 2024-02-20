using Cysharp.Threading.Tasks;

namespace CS2LuaDoc.Core;

public class LuaAnnotationBuilder
{
    public ProjectMetaData ProjectMetaData { get; private set; } = null!;
    public string OutputPath { get; private set; } = string.Empty;
    
    public LuaAnnotationBuilder SetProjectMetaData(ProjectMetaData projectMetaData)
    {
        ProjectMetaData = projectMetaData;
        return this;
    }
    
    public LuaAnnotationBuilder SetOutputPath(string outputPath)
    {
        OutputPath = outputPath;
        return this;
    }
    
    public async UniTask BuildAsync()
    {
        if (ProjectMetaData == null)
        {
            throw new Exception("ProjectMetaData is null");
        }
        
        if (string.IsNullOrEmpty(OutputPath))
        {
            throw new Exception("OutputPath is null");
        }
        
        var luaAnnotationGenerator = new LuaAnnotationGenerator();
        await luaAnnotationGenerator.Generate(ProjectMetaData, OutputPath);
    } 
}