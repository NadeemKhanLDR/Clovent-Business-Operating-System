namespace Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;

/// <summary>A quick-order template expanded for display: names and prices resolved against the current catalog read model.</summary>
public sealed record QuickOrderTemplateDto(
    Guid TemplateId,
    string Name,
    string? Description,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyCollection<QuickOrderTemplateItemDto> Items,
    decimal TotalPrice, Guid? WarehouseId = null);

