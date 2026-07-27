using Spectre.Console;
using Spectre.Console.Cli;

namespace Clovent.CLI.Commands.Init;

public sealed class InitCommand : Command
{
    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]Init command executed.[/]");
        return 0;
    }
}
