using Spectre.Console;
using Spectre.Console.Cli;

namespace Clovent.CLI.Commands.Module;

public sealed class ModuleCommand : Command
{
    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]Module command executed.[/]");
        return 0;
    }
}
