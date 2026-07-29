using Clovent.Domain;

namespace Clovent.Inventory.WarehouseStocks.Events;

/// <summary>Raised when a reservation against a <see cref="WarehouseStock"/> balance is released.</summary>
public sealed record StockReservationReleased(WarehouseStockId WarehouseStockId, decimal Quantity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
