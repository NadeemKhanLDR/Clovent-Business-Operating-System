using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.Restaurant.DiningAreas.Events;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Clovent.Restaurant.Shared;

namespace Clovent.Restaurant.DiningAreas;

/// <summary>
/// A named floor-plan zone within a <see cref="Branch"/> (e.g. "Patio",
/// "Main Hall", "Bar") that <see cref="Tables.Table"/>s belong to.
/// References its owning branch by id only, the same pattern every
/// Branch-scoped MasterData aggregate (Department/Warehouse/Terminal)
/// already established.
/// </summary>
public sealed class DiningArea : AggregateRoot<DiningAreaId>
{
    /// <summary>The branch this dining area belongs to, fixed at creation.</summary>
    public BranchId BranchId { get; }

    /// <summary>The dining area's display name.</summary>
    public DiningAreaName Name { get; private set; }

    /// <summary>The dining area's current lifecycle state.</summary>
    public RestaurantStatus Status { get; private set; }

    /// <summary>UTC instant this dining area was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private DiningArea(DiningAreaId id, BranchId branchId, DiningAreaName name, RestaurantStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        BranchId = branchId;
        Name = name;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active dining area under the given branch.</summary>
    public static DiningArea Create(BranchId branchId, DiningAreaName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var area = new DiningArea(DiningAreaId.New(), branchId, name, RestaurantStatus.Active, now);
        area.AddDomainEvent(new DiningAreaCreated(area.Id, area.BranchId, area.Name, now));
        return area;
    }

    /// <summary>Renames the dining area. A no-op (no event raised) if unchanged.</summary>
    public void Rename(DiningAreaName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new DiningAreaRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the dining area.</summary>
    /// <exception cref="RestaurantDomainException">The dining area is already active.</exception>
    public void Activate()
    {
        if (Status == RestaurantStatus.Active)
            throw RestaurantDomainException.DiningAreaAlreadyActive(Id);

        Status = RestaurantStatus.Active;
        AddDomainEvent(new DiningAreaActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the dining area.</summary>
    /// <exception cref="RestaurantDomainException">The dining area is not active.</exception>
    public void Deactivate()
    {
        if (Status != RestaurantStatus.Active)
            throw RestaurantDomainException.DiningAreaNotActive(Id);

        Status = RestaurantStatus.Inactive;
        AddDomainEvent(new DiningAreaDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
