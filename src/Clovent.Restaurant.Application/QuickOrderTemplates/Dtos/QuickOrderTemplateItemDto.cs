namespace Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;

/// <summary>One expanded line of a quick-order template, with its resolved unit price.</summary>
public sealed record QuickOrderTemplateItemDto(
    Guid VariantId,
    string ProductName,
    string VariantName,
    decimal Quantity,
    decimal UnitPrice,
    decimal? TemplateUnitPrice = null)
{
    /// <summary>This item's resolved total: <see cref="Quantity"/> times <see cref="UnitPrice"/>.</summary>
    public decimal Total => Quantity * UnitPrice;
}
