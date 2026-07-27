using Spectre.Console;
using Spectre.Console.Cli;

namespace Clovent.CLI.Commands.Shared;

public abstract class CommandBase : Command
{
    protected void Success(string message)
        => AnsiConsole.MarkupLine($"[green]{message}[/]");

    protected void Warning(string message)
        => AnsiConsole.MarkupLine($"[yellow]{message}[/]");

    protected void Error(string message)
        => AnsiConsole.MarkupLine($"[red]{message}[/]");

    protected void Info(string message)
        => AnsiConsole.MarkupLine($"[deepskyblue1]{message}[/]");
}
