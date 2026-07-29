using Clovent.Domain;

namespace Clovent.Inventory.WarehouseStocks.Events;

/// <summary>Raised when stock is issued out of a <see cref="WarehouseStock"/> balance.</summary>
public sealed record StockIssued(WarehouseStockId WarehouseStockId, decimal Quantity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
