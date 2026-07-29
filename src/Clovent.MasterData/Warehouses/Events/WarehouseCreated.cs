using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses.ValueObjects;

namespace Clovent.MasterData.Warehouses.Events;

/// <summary>Raised when a new <see cref="Warehouse"/> is created.</summary>
public sealed record WarehouseCreated(WarehouseId WarehouseId, BranchId BranchId, WarehouseName Name, EntityCode Code, DateTimeOffset OccurredOnUtc) : IDomainEvent;
