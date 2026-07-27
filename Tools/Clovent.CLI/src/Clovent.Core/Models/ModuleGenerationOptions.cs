namespace Clovent.Core.Models;

public sealed class ModuleGenerationOptions
{
    public required string Name { get; set; }
    public string? OutputPath { get; set; }
    public bool IncludeUiLayer { get; set; } = true;
    public bool IncludeDocumentation { get; set; } = true;
}
