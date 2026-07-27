using Spectre.Console;
using Spectre.Console.Cli;

namespace Clovent.CLI.Commands.Docs;

public sealed class DocsCommand : Command
{
    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]Docs command executed.[/]");
        return 0;
    }
}
