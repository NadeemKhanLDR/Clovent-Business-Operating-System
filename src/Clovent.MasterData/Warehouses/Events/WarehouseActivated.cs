using Clovent.Domain;

namespace Clovent.MasterData.Warehouses.Events;

/// <summary>Raised when a <see cref="Warehouse"/> is (re)activated.</summary>
public sealed record WarehouseActivated(WarehouseId WarehouseId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
