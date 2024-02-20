using System.Diagnostics;
using CS2LuaDoc.Core;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace TestLibrary;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var slnPath = "D:/codes/csharp/CS2LuaDoc/CS2LuaDoc.sln";
        
        var solutionLoader = new SolutionLoader(slnPath);
        await solutionLoader.LoadAsync();
        var csMetaDataLoader = new CSMetaDataLoader(solutionLoader);
        var projectMetaData = await csMetaDataLoader.LoadAsync();
        var luaAnnotationBuilder = new LuaAnnotationBuilder();
        
        Directory.Delete("D:/codes/csharp/CS2LuaDoc/output/", true);
        Directory.CreateDirectory("D:/codes/csharp/CS2LuaDoc/output/");
        
        await luaAnnotationBuilder.SetProjectMetaData(projectMetaData)
            .SetOutputPath("D:/codes/csharp/CS2LuaDoc/output/")
            .BuildAsync();
        
        Console.WriteLine("Build Success");
        return 0;
    }
}