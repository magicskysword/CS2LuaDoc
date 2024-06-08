using CS2LuaDoc.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CS2LuaDoc.Cli;

public class GenerateLuaDocCommand : AsyncCommand<GenerateLuaDocCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[SolutionPath]")]
        public string? SolutionPath { get; set; }
        
        [CommandOption("-o|--output <OutputPath>")]
        public string? OutputPath { get; set; }
        
        [CommandOption("--exclude-namespace <ExcludeNamespace>")]
        public string? ExcludeNamespace { get; set; }
        
        [CommandOption("--exclude-assembly <ExcludeAssembly>")]
        public string? ExcludeAssembly { get; set; }
        
        [CommandOption("--include-assembly <IncludeAssembly>")]
        public string? IncludeAssembly { get; set; }
        
        [CommandOption("-p|--public-only", IsHidden = true)]
        public bool? IsPublicOnly { get; set; }
        
        public string[] ExcludeNamespaceArray => ExcludeNamespace?.Split(',') ?? Array.Empty<string>();
        public string[] ExcludeAssemblyArray => ExcludeAssembly?.Split(',') ?? Array.Empty<string>();
        public string[] IncludeAssemblyArray => IncludeAssembly?.Split(',') ?? Array.Empty<string>();
        
        public override ValidationResult Validate()
        {
            if (string.IsNullOrEmpty(SolutionPath))
            {
                return ValidationResult.Error("SolutionPath is null");
            }
            
            if (SolutionPath.StartsWith('"') && SolutionPath.EndsWith('"'))
            {
                SolutionPath = SolutionPath[1..^1];
            }
            
            if (!string.IsNullOrEmpty(OutputPath) && OutputPath.StartsWith('"') && OutputPath.EndsWith('"'))
            {
                OutputPath = OutputPath[1..^1];
            }

            return ValidationResult.Success();
        }
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (!File.Exists(settings.SolutionPath))
        {
            return ValidationResult.Error("SolutionPath is not exist");
        }
        
        return ValidationResult.Success();
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (string.IsNullOrEmpty(settings.OutputPath))
        {
            settings.OutputPath = Path.Combine(Path.GetDirectoryName(settings.SolutionPath)!, "output");
        }
        
        var excludeNamespace = settings.ExcludeNamespaceArray;
        var excludeAssembly = settings.ExcludeAssemblyArray;
        var includeAssembly = settings.IncludeAssemblyArray;
        
        AnsiConsole.MarkupLine($"[bold]SolutionPath:[/]{settings.SolutionPath}");
        AnsiConsole.MarkupLine($"[bold]OutputPath:[/]{settings.OutputPath}");
        AnsiConsole.MarkupLine($"[bold]IsPublicOnly:[/]{settings.IsPublicOnly}");
        AnsiConsole.MarkupLine($"[bold]ExcludeNamespace:[/]{excludeNamespace.Length}");
        foreach (var ns in excludeNamespace)
        {
            AnsiConsole.MarkupLine($"[bold]  -[/]{ns}");
        }
        AnsiConsole.MarkupLine($"[bold]ExcludeAssembly:[/]{excludeAssembly.Length}");
        foreach (var assembly in excludeAssembly)
        {
            AnsiConsole.MarkupLine($"[bold]  -[/]{assembly}");
        }
        AnsiConsole.MarkupLine($"[bold]IncludeAssembly:[/]{includeAssembly.Length}");
        foreach (var assembly in includeAssembly)
        {
            AnsiConsole.MarkupLine($"[bold]  -[/]{assembly}");
        }
        
        // Load C# MetaData
        var csMetaDataLoader = new CSMetaDataLoaderBuilder()
            .SetSolutionPath(settings.SolutionPath!)
            .SetExcludeAssembly(excludeAssembly)
            .SetIncludeAssembly(includeAssembly)
            .Build();
        var projectMetaData = await csMetaDataLoader.LoadAsync();

        if (Directory.Exists(settings.OutputPath))
        {
            Directory.Delete(settings.OutputPath, true);
        }
        Directory.CreateDirectory(settings.OutputPath);
        
        // Generate Lua Annotation
        var luaAnnotationBuilder = new LuaAnnotationGeneratorBuilder();
        luaAnnotationBuilder.SetProjectMetaData(projectMetaData)
            .SetOutputPath(settings.OutputPath)
            .AddClassFilters(
                LuaAnnotationClassNamespaceExcludeFilter.CreateExcludeFilters(excludeNamespace));

        if(settings.IsPublicOnly == true)
        {
            luaAnnotationBuilder.AddClassFilter(new LuaAnnotationClassPublicFilter());
        }
        
        var generator = luaAnnotationBuilder.Build();
        await generator.GenerateAsync();
        
        Console.WriteLine("Build Success");
        
        return 0;
    }
}