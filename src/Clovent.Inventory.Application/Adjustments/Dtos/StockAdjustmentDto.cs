using Clovent.Inventory.Adjustments;

namespace Clovent.Inventory.Application.Adjustments.Dtos;

/// <summary>Read-model shape for a <see cref="StockAdjustment"/>, safe to cross a process boundary.</summary>
public sealed record StockAdjustmentDto(
    Guid StockAdjustmentId,
    Guid WarehouseId,
    Guid ProductVariantId,
    string AdjustmentType,
    decimal Quantity,
    string Reason,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? AppliedAtUtc)
{
    /// <summary>Projects a domain <see cref="StockAdjustment"/> into its DTO.</summary>
    public static StockAdjustmentDto FromDomain(StockAdjustment adjustment) => new(
        adjustment.Id.Value,
        adjustment.WarehouseId.Value,
        adjustment.ProductVariantId.Value,
        adjustment.AdjustmentType.ToString(),
        adjustment.Quantity,
        adjustment.Reason,
        adjustment.Status.ToString(),
        adjustment.CreatedAtUtc,
        adjustment.AppliedAtUtc);
}
