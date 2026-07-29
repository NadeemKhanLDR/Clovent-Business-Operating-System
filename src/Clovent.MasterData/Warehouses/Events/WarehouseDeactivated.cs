using Clovent.Domain;

namespace Clovent.MasterData.Warehouses.Events;

/// <summary>Raised when a <see cref="Warehouse"/> is deactivated.</summary>
public sealed record WarehouseDeactivated(WarehouseId WarehouseId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
