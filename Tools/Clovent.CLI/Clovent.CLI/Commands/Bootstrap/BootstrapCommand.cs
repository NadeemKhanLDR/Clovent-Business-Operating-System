namespace Clovent.CLI.Commands.Bootstrap;

using Clovent.Core.Interfaces;
using Clovent.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;

public sealed class BootstrapCommand : Command
{
    private readonly IBootstrapService _bootstrapService;

    public BootstrapCommand(IBootstrapService bootstrapService)
    {
        _bootstrapService = bootstrapService;
    }

    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        var rootPath = Directory.GetCurrentDirectory();
        var result = _bootstrapService.Execute(new BootstrapOptions
        {
            RootPath = rootPath
        });

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
}
