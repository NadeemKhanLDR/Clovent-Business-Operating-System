using Spectre.Console;
using Spectre.Console.Cli;

namespace Clovent.CLI.Commands.Build;

public sealed class BuildCommand : Command
{
    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]Build command executed.[/]");
        return 0;
    }
}
