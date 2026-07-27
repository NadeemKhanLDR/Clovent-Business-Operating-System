namespace Clovent.CLI.Models.Entity;

using System.ComponentModel;
using Spectre.Console.Cli;

public sealed class EntitySettings : CommandSettings
{
    [CommandArgument(0, "<module>")]
    [Description("Name of the module to contain the entity.")]
    public string ModuleName { get; init; } = string.Empty;

    [CommandArgument(1, "<name>")]
    [Description("Name of the entity/aggregate root.")]
    public string EntityName { get; init; } = string.Empty;

    [CommandOption("-o|--output")]
    [Description("Output root directory. Defaults to current directory.")]
    public string? Output { get; init; }

    [CommandOption("-p|--properties")]
    [Description("Entity properties in format Name:Type,Name:Type (e.g. Name:string,Price:decimal).")]
    public string? Properties { get; init; }
}
