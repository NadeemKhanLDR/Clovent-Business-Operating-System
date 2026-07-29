using Clovent.Domain;

namespace Clovent.Inventory.WarehouseStocks.Events;

/// <summary>Raised when quantity is reserved against a <see cref="WarehouseStock"/> balance.</summary>
public sealed record StockReserved(WarehouseStockId WarehouseStockId, decimal Quantity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
