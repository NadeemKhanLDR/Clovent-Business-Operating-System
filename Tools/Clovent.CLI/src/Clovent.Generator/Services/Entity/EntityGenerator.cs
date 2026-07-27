namespace Clovent.Generator.Services.Entity;

using Clovent.Core.Models;
using Clovent.Core.Results;
using Clovent.Documents;
using Clovent.Templates;

public sealed class EntityGenerator : IEntityGenerator
{
    private readonly ITemplateEngine _templateEngine;
    private readonly IDocumentGenerator _documentGenerator;

    public EntityGenerator(ITemplateEngine templateEngine, IDocumentGenerator documentGenerator)
    {
        _templateEngine = templateEngine;
        _documentGenerator = documentGenerator;
    }

    public GenerationResult Generate(EntityGenerationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ModuleName))
        {
            return GenerationResult.Fail("Module name is required.");
        }

        if (string.IsNullOrWhiteSpace(options.EntityName))
        {
            return GenerationResult.Fail("Entity name is required.");
        }

        var output = string.IsNullOrWhiteSpace(options.OutputPath)
            ? Directory.GetCurrentDirectory()
            : options.OutputPath;

        var moduleRoot = Path.Combine(output, options.ModuleName);
        if (!Directory.Exists(moduleRoot))
        {
            Directory.CreateDirectory(moduleRoot);
        }

        var createdFiles = new List<string>();

        // 1. Domain Entity & Id ValueObject & Event
        var domainEntitiesDir = Path.Combine(moduleRoot, "Domain", "Entities");
        Directory.CreateDirectory(domainEntitiesDir);
        var entityFile = Path.Combine(domainEntitiesDir, $"{options.EntityName}.cs");
        File.WriteAllText(entityFile, _templateEngine.RenderEntityClass(options));
        createdFiles.Add(entityFile);

        var domainVoDir = Path.Combine(moduleRoot, "Domain", "ValueObjects");
        Directory.CreateDirectory(domainVoDir);
        var voFile = Path.Combine(domainVoDir, $"{options.EntityName}Id.cs");
        File.WriteAllText(voFile, _templateEngine.RenderEntityIdValueObject(options));
        createdFiles.Add(voFile);

        var domainEventsDir = Path.Combine(moduleRoot, "Domain", "Events");
        Directory.CreateDirectory(domainEventsDir);
        var eventFile = Path.Combine(domainEventsDir, $"{options.EntityName}CreatedEvent.cs");
        File.WriteAllText(eventFile, _templateEngine.RenderDomainEventClass(options, "Created"));
        createdFiles.Add(eventFile);

        // 2. Application CQRS Commands & Queries & Handlers
        if (options.GenerateCqrs)
        {
            var appCmdDir = Path.Combine(moduleRoot, "Application", "Commands");
            Directory.CreateDirectory(appCmdDir);
            var cmdFile = Path.Combine(appCmdDir, $"Create{options.EntityName}Command.cs");
            File.WriteAllText(cmdFile, _templateEngine.RenderCreateCommand(options));
            createdFiles.Add(cmdFile);

            var appQueryDir = Path.Combine(moduleRoot, "Application", "Queries");
            Directory.CreateDirectory(appQueryDir);
            var queryFile = Path.Combine(appQueryDir, $"Get{options.EntityName}ByIdQuery.cs");
            File.WriteAllText(queryFile, _templateEngine.RenderGetByIdQuery(options));
            createdFiles.Add(queryFile);

            var appHandlersDir = Path.Combine(moduleRoot, "Application", "Handlers");
            Directory.CreateDirectory(appHandlersDir);
            var cmdHandlerFile = Path.Combine(appHandlersDir, $"Create{options.EntityName}CommandHandler.cs");
            File.WriteAllText(cmdHandlerFile, _templateEngine.RenderCreateCommandHandler(options));
            createdFiles.Add(cmdHandlerFile);

            var queryHandlerFile = Path.Combine(appHandlersDir, $"Get{options.EntityName}ByIdQueryHandler.cs");
            File.WriteAllText(queryHandlerFile, _templateEngine.RenderGetByIdQueryHandler(options));
            createdFiles.Add(queryHandlerFile);
        }

        // 3. Infrastructure Persistence EF Configuration
        if (options.GenerateEfConfiguration)
        {
            var efDir = Path.Combine(moduleRoot, "Infrastructure", "Persistence", "Configurations");
            Directory.CreateDirectory(efDir);
            var efFile = Path.Combine(efDir, $"{options.EntityName}Configuration.cs");
            File.WriteAllText(efFile, _templateEngine.RenderEfCoreConfiguration(options));
            createdFiles.Add(efFile);
        }

        // 4. DevExpress WinForms View
        if (options.GenerateWinFormsView)
        {
            var uiDir = Path.Combine(moduleRoot, "UI", "Views");
            Directory.CreateDirectory(uiDir);
            var uiFile = Path.Combine(uiDir, $"{options.EntityName}View.cs");
            File.WriteAllText(uiFile, _templateEngine.RenderDevExpressView(options));
            createdFiles.Add(uiFile);
        }

        // 5. Obsidian Documentation
        var docContent = _templateEngine.RenderObsidianEntityDoc(options);
        _documentGenerator.GenerateEntityDocumentation(output, options, docContent);

        return GenerationResult.Ok(createdFiles, $"Entity '{options.EntityName}' created successfully in module '{options.ModuleName}'.");
    }
}
