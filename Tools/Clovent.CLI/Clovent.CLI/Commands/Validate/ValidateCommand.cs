using Spectre.Console;
using Spectre.Console.Cli;

namespace Clovent.CLI.Commands.Validate;

public sealed class ValidateCommand : Command
{
    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]Validate command executed.[/]");
        return 0;
    }
}
