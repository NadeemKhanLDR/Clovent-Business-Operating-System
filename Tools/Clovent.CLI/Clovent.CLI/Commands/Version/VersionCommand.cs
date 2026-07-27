using Spectre.Console;
using Spectre.Console.Cli;

namespace Clovent.CLI.Commands.Version;

public sealed class VersionCommand : Command
{
    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]Version command executed.[/]");
        return 0;
    }
}
