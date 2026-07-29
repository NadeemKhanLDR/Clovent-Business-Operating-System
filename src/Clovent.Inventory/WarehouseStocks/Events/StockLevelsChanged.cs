using Clovent.Domain;

namespace Clovent.Inventory.WarehouseStocks.Events;

/// <summary>Raised when a <see cref="WarehouseStock"/>'s minimum/maximum stock levels are changed.</summary>
public sealed record StockLevelsChanged(WarehouseStockId WarehouseStockId, decimal MinimumStock, decimal MaximumStock, DateTimeOffset OccurredOnUtc) : IDomainEvent;
