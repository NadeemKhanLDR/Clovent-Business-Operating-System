namespace Clovent.CLI.Commands.Shared;

using Clovent.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

public sealed class DoctorCommand : Command
{
    private readonly IDoctorService _doctorService;

    public DoctorCommand(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        var rootPath = Directory.GetCurrentDirectory();
        var result = _doctorService.Diagnose(rootPath);

        var table = new Table();
        table.AddColumn("Check");
        table.AddColumn("Status");
        table.AddColumn("Details");

        foreach (var item in result.Checks)
        {
            var status = item.IsPassed ? "[green]PASS[/]" : "[red]FAIL[/]";
            table.AddRow(item.Name, status, item.Details);
        }

        AnsiConsole.Write(table);

        return result.IsHealthy ? 0 : -1;
    }
}
