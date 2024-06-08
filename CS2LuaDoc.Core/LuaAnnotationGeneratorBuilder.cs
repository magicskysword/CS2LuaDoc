using Cysharp.Threading.Tasks;

namespace CS2LuaDoc.Core;

public class LuaAnnotationGeneratorBuilder
{
    public ProjectMetaData ProjectMetaData { get; private set; } = null!;
    public string OutputPath { get; private set; } = string.Empty;
    public List<LuaAnnotationClassFilter> ClassFilters { get; private set; } = new();
    
    public LuaAnnotationGeneratorBuilder SetProjectMetaData(ProjectMetaData projectMetaData)
    {
        ProjectMetaData = projectMetaData;
        return this;
    }
    
    public LuaAnnotationGeneratorBuilder SetOutputPath(string outputPath)
    {
        OutputPath = outputPath;
        return this;
    }
    
    public LuaAnnotationGeneratorBuilder AddClassFilter(LuaAnnotationClassFilter classFilter)
    {
        ClassFilters.Add(classFilter);
        return this;
    }
    
    public LuaAnnotationGeneratorBuilder AddClassFilters(IEnumerable<LuaAnnotationClassFilter> classFilters)
    {
        ClassFilters.AddRange(classFilters);
        return this;
    }
    
    public LuaAnnotationGenerator Build()
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
        luaAnnotationGenerator.ClassFilters.AddRange(ClassFilters);
        luaAnnotationGenerator.ProjectMetaData = ProjectMetaData;
        luaAnnotationGenerator.OutputPath = OutputPath;
        
        return luaAnnotationGenerator;
    } 
}