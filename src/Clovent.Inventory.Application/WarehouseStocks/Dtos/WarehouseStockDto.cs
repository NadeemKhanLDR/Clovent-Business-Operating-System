using Clovent.Inventory.WarehouseStocks;

namespace Clovent.Inventory.Application.WarehouseStocks.Dtos;

/// <summary>Read-model shape for a <see cref="WarehouseStock"/> balance, safe to cross a process boundary.</summary>
public sealed record WarehouseStockDto(
    Guid WarehouseStockId,
    Guid WarehouseId,
    Guid ProductVariantId,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal QuantityAvailable,
    decimal MinimumStock,
    decimal MaximumStock,
    bool AllowNegativeStock,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    /// <summary>Projects a domain <see cref="WarehouseStock"/> into its DTO.</summary>
    public static WarehouseStockDto FromDomain(WarehouseStock stock) => new(
        stock.Id.Value,
        stock.WarehouseId.Value,
        stock.ProductVariantId.Value,
        stock.QuantityOnHand,
        stock.QuantityReserved,
        stock.QuantityAvailable,
        stock.MinimumStock,
        stock.MaximumStock,
        stock.AllowNegativeStock,
        stock.CreatedAtUtc,
        stock.UpdatedAtUtc);
}
