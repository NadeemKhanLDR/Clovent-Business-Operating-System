using Spectre.Console.Cli;
using System.ComponentModel;

namespace Clovent.CLI.Models.Module;

public sealed class ModuleSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("Name of the module.")]
    public string Name { get; init; } = string.Empty;

    [CommandOption("-o|--output")]
    [Description("Output directory. Defaults to the current directory.")]
    public string? Output { get; init; }
}
