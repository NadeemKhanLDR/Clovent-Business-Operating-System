namespace Clovent.Generator.Services.Module;

using Clovent.Core.Models;
using Clovent.Core.Results;
using Clovent.Documents;
using Clovent.Templates;

public sealed class ModuleGenerator : IModuleGenerator
{
    private readonly ITemplateEngine _templateEngine;
    private readonly IDocumentGenerator _documentGenerator;

    public ModuleGenerator(ITemplateEngine templateEngine, IDocumentGenerator documentGenerator)
    {
        _templateEngine = templateEngine;
        _documentGenerator = documentGenerator;
    }

    public GenerationResult Generate(ModuleGenerationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Name))
        {
            return GenerationResult.Fail("Module name cannot be empty.");
        }

        var output = string.IsNullOrWhiteSpace(options.OutputPath)
            ? Directory.GetCurrentDirectory()
            : options.OutputPath;

        var moduleRoot = Path.Combine(output, options.Name);
        var createdFiles = new List<string>();

        var folders = new[]
        {
            "Domain",
            "Domain/Aggregates",
            "Domain/Entities",
            "Domain/ValueObjects",
            "Domain/Events",
            "Domain/Specifications",
            "Domain/Services",

            "Application",
            "Application/Commands",
            "Application/Queries",
            "Application/DTOs",
            "Application/Validators",
            "Application/Handlers",

            "Infrastructure",
            "Infrastructure/Persistence",
            "Infrastructure/Persistence/Configurations",

            "UI",
            "UI/Views",

            "Documentation",
            "Tests"
        };

        foreach (var folder in folders)
        {
            Directory.CreateDirectory(Path.Combine(moduleRoot, folder));
        }

        // Generate README
        var readmePath = Path.Combine(moduleRoot, "README.md");
        File.WriteAllText(readmePath, $"# {options.Name} Module\n\nGenerated with Clovent Business Operating System CLI.");
        createdFiles.Add(readmePath);

        // Generate Documentation note if enabled
        if (options.IncludeDocumentation)
        {
            var docContent = _templateEngine.RenderObsidianModuleDoc(options);
            _documentGenerator.GenerateModuleDocumentation(output, options, docContent);
        }

        return GenerationResult.Ok(createdFiles, $"Module '{options.Name}' generated successfully.");
    }
}
