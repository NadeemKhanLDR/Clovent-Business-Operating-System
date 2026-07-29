using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.WarehouseStocks.Events;

/// <summary>Raised when a new <see cref="WarehouseStock"/> balance record is created.</summary>
public sealed record WarehouseStockCreated(WarehouseStockId WarehouseStockId, WarehouseId WarehouseId, ProductVariantId ProductVariantId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
