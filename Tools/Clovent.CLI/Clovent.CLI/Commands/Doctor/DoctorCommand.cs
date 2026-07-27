using Spectre.Console;
using Spectre.Console.Cli;

namespace Clovent.CLI.Commands.Doctor;

public sealed class DoctorCommand : Command
{
    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]Doctor command executed.[/]");
        return 0;
    }
}
