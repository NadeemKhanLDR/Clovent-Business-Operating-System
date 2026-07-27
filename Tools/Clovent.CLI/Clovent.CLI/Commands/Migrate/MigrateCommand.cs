using Spectre.Console;
using Spectre.Console.Cli;

namespace Clovent.CLI.Commands.Migrate;

public sealed class MigrateCommand : Command
{
    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]Migrate command executed.[/]");
        return 0;
    }
}
