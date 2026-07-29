using Clovent.Domain;

namespace Clovent.Inventory.WarehouseStocks.Events;

/// <summary>Raised when a <see cref="WarehouseStock"/>'s negative-stock policy is changed.</summary>
public sealed record NegativeStockPolicyChanged(WarehouseStockId WarehouseStockId, bool AllowNegativeStock, DateTimeOffset OccurredOnUtc) : IDomainEvent;
