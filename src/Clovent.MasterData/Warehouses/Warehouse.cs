using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.MasterData.Shared;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses.Events;
using Clovent.MasterData.Warehouses.ValueObjects;

namespace Clovent.MasterData.Warehouses;

/// <summary>A physical or logical stock-holding location within a <see cref="Branch"/>. References its owning branch by id only - see <see cref="Departments.Department"/>'s identical doc comment.</summary>
public sealed class Warehouse : AggregateRoot<WarehouseId>
{
    /// <summary>The branch this warehouse belongs to, fixed at creation.</summary>
    public BranchId BranchId { get; }

    /// <summary>The warehouse's display name.</summary>
    public WarehouseName Name { get; private set; }

    /// <summary>The warehouse's short code (e.g. "WH-01").</summary>
    public EntityCode Code { get; }

    /// <summary>The warehouse's current lifecycle state.</summary>
    public MasterDataStatus Status { get; private set; }

    /// <summary>UTC instant this warehouse was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Warehouse(WarehouseId id, BranchId branchId, WarehouseName name, EntityCode code, MasterDataStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        BranchId = branchId;
        Name = name;
        Code = code;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active warehouse under the given branch.</summary>
    public static Warehouse Create(BranchId branchId, WarehouseName name, EntityCode code)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(code);

        var now = DateTimeOffset.UtcNow;
        var warehouse = new Warehouse(WarehouseId.New(), branchId, name, code, MasterDataStatus.Active, now);
        warehouse.AddDomainEvent(new WarehouseCreated(warehouse.Id, warehouse.BranchId, warehouse.Name, warehouse.Code, now));
        return warehouse;
    }

    /// <summary>Renames the warehouse. A no-op (no event raised) if unchanged.</summary>
    public void Rename(WarehouseName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new WarehouseRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the warehouse.</summary>
    /// <exception cref="MasterDataDomainException">The warehouse is already active.</exception>
    public void Activate()
    {
        if (Status == MasterDataStatus.Active)
            throw MasterDataDomainException.WarehouseAlreadyActive(Id);

        Status = MasterDataStatus.Active;
        AddDomainEvent(new WarehouseActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the warehouse.</summary>
    /// <exception cref="MasterDataDomainException">The warehouse is not active.</exception>
    public void Deactivate()
    {
        if (Status != MasterDataStatus.Active)
            throw MasterDataDomainException.WarehouseNotActive(Id);

        Status = MasterDataStatus.Inactive;
        AddDomainEvent(new WarehouseDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
