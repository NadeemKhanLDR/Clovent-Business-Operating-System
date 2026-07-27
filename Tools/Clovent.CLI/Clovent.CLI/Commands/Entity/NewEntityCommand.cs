namespace Clovent.CLI.Commands.Entity;

using Clovent.CLI.Models.Entity;
using Clovent.Core.Models;
using Clovent.Generator.Services.Entity;
using Spectre.Console;
using Spectre.Console.Cli;

public sealed class NewEntityCommand : Command<EntitySettings>
{
    private readonly IEntityGenerator _generator;

    public NewEntityCommand(IEntityGenerator generator)
    {
        _generator = generator;
    }

    protected override int Execute(
        CommandContext context,
        EntitySettings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ModuleName))
        {
            AnsiConsole.MarkupLine("[red]Module name is required.[/]");
            return -1;
        }

        if (string.IsNullOrWhiteSpace(settings.EntityName))
        {
            AnsiConsole.MarkupLine("[red]Entity name is required.[/]");
            return -1;
        }

        var properties = ParseProperties(settings.Properties);

        var options = new EntityGenerationOptions
        {
            ModuleName = settings.ModuleName,
            EntityName = settings.EntityName,
            OutputPath = settings.Output,
            Properties = properties
        };

        var result = _generator.Generate(options);

        if (!result.Success)
        {
            foreach (var err in result.Errors)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {err}");
            }
            return -1;
        }

        foreach (var msg in result.Messages)
        {
            AnsiConsole.MarkupLine($"[green]{msg}[/]");
        }

        return 0;
    }

    private static List<EntityPropertyDefinition> ParseProperties(string? propsString)
    {
        var list = new List<EntityPropertyDefinition>();
        if (string.IsNullOrWhiteSpace(propsString)) return list;

        var parts = propsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var propParts = part.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (propParts.Length >= 2)
            {
                list.Add(new EntityPropertyDefinition
                {
                    Name = propParts[0].Trim(),
                    Type = propParts[1].Trim()
                });
            }
            else if (propParts.Length == 1)
            {
                list.Add(new EntityPropertyDefinition
                {
                    Name = propParts[0].Trim(),
                    Type = "string"
                });
            }
        }
        return list;
    }
}
