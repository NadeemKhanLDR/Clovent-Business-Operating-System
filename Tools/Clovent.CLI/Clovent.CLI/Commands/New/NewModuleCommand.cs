namespace Clovent.CLI.Commands.New;

using Clovent.CLI.Models.Module;
using Clovent.Core.Models;
using Clovent.Generator.Services.Module;
using Spectre.Console;
using Spectre.Console.Cli;

public sealed class NewModuleCommand : Command<ModuleSettings>
{
    private readonly IModuleGenerator _generator;

    public NewModuleCommand(IModuleGenerator generator)
    {
        _generator = generator;
    }

    protected override int Execute(
        CommandContext context,
        ModuleSettings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Module name is required.[/]");
            return -1;
        }

        var result = _generator.Generate(new ModuleGenerationOptions
        {
            Name = settings.Name,
            OutputPath = settings.Output
        });

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {error}");
            }
            return -1;
        }

        foreach (var msg in result.Messages)
        {
            AnsiConsole.MarkupLine($"[green]{msg}[/]");
        }

        return 0;
    }
}