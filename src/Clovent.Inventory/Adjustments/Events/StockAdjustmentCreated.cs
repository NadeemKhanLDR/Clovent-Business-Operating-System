using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Adjustments.Events;

/// <summary>Raised when a new <see cref="StockAdjustment"/> is proposed.</summary>
public sealed record StockAdjustmentCreated(
    StockAdjustmentId StockAdjustmentId,
    WarehouseId WarehouseId,
    ProductVariantId ProductVariantId,
    StockAdjustmentType AdjustmentType,
    decimal Quantity,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
