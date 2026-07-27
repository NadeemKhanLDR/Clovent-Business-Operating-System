namespace Clovent.Core.Models;

public sealed class EntityPropertyDefinition
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public bool IsRequired { get; set; } = true;
    public int? MaxLength { get; set; }
}

public sealed class EntityGenerationOptions
{
    public required string ModuleName { get; set; }
    public required string EntityName { get; set; }
    public string? OutputPath { get; set; }
    public List<EntityPropertyDefinition> Properties { get; set; } = new();
    public bool IsAggregateRoot { get; set; } = true;
    public bool GenerateCqrs { get; set; } = true;
    public bool GenerateEfConfiguration { get; set; } = true;
    public bool GenerateWinFormsView { get; set; } = true;
}
