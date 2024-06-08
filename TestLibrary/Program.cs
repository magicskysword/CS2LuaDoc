using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using CS2LuaDoc.Core;


namespace TestLibrary;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var slnPath = "D:/code/csharp/CS2LuaDoc/CS2LuaDoc.sln";
        var outDir = "D:/code/csharp/CS2LuaDoc/output/";
        
        var solutionLoader = new SolutionLoader(slnPath);
        await solutionLoader.LoadAsync();
        var csMetaDataLoader = new CSMetaDataLoader(solutionLoader);
        var projectMetaData = await csMetaDataLoader.LoadAsync();
        var luaAnnotationBuilder = new LuaAnnotationGeneratorBuilder();

        if (Directory.Exists(outDir))
        {
            Directory.Delete(outDir, true);
        }
        Directory.CreateDirectory(outDir);

        var generator = luaAnnotationBuilder.SetProjectMetaData(projectMetaData)
            .SetOutputPath("D:/code/csharp/CS2LuaDoc/output/")
            .Build();
        
        await generator.GenerateAsync();
        
        Console.WriteLine("Build Success");
        return 0;
    }
}