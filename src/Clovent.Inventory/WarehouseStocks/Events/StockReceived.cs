using Clovent.Domain;

namespace Clovent.Inventory.WarehouseStocks.Events;

/// <summary>Raised when stock is received into a <see cref="WarehouseStock"/> balance.</summary>
public sealed record StockReceived(WarehouseStockId WarehouseStockId, decimal Quantity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
