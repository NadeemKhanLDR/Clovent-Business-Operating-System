using Clovent.Domain;
using Clovent.MasterData.Warehouses.ValueObjects;

namespace Clovent.MasterData.Warehouses.Events;

/// <summary>Raised when a <see cref="Warehouse"/>'s name changes.</summary>
public sealed record WarehouseRenamed(WarehouseId WarehouseId, WarehouseName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
