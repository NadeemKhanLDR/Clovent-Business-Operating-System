using Clovent.Domain;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Shared;
using Clovent.Restaurant.Tables.Events;

namespace Clovent.Restaurant.Tables;

/// <summary>
/// A physical seating location within a <see cref="DiningAreas.DiningArea"/>.
/// Carries two independent statuses: <see cref="Status"/> (lifecycle -
/// does this table exist on the floor plan at all, the same
/// Active/Inactive vocabulary every reference-data aggregate in this
/// solution uses) and <see cref="OccupancyStatus"/> (real-time floor state -
/// Available/Occupied/Reserved/OutOfService). References its owning dining
/// area by id only, the same pattern every parent/child relationship in
/// this solution already established.
/// </summary>
public sealed class Table : AggregateRoot<TableId>
{
    /// <summary>The dining area this table belongs to, fixed at creation.</summary>
    public DiningAreaId DiningAreaId { get; }

    /// <summary>The table's short code (e.g. "T-01"), fixed at creation.</summary>
    public EntityCode Code { get; }

    /// <summary>The table's seating capacity.</summary>
    public int Capacity { get; private set; }

    /// <summary>The table's current lifecycle state.</summary>
    public RestaurantStatus Status { get; private set; }

    /// <summary>The table's current real-time floor state.</summary>
    public TableOccupancyStatus OccupancyStatus { get; private set; }

    /// <summary>UTC instant this table was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Table(
        TableId id,
        DiningAreaId diningAreaId,
        EntityCode code,
        int capacity,
        RestaurantStatus status,
        TableOccupancyStatus occupancyStatus,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        DiningAreaId = diningAreaId;
        Code = code;
        Capacity = capacity;
        Status = status;
        OccupancyStatus = occupancyStatus;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active, available table under the given dining area.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not positive.</exception>
    public static Table Create(DiningAreaId diningAreaId, EntityCode code, int capacity)
    {
        ArgumentNullException.ThrowIfNull(code);
        RequirePositiveCapacity(capacity);

        var now = DateTimeOffset.UtcNow;
        var table = new Table(TableId.New(), diningAreaId, code, capacity, RestaurantStatus.Active, TableOccupancyStatus.Available, now);
        table.AddDomainEvent(new TableCreated(table.Id, table.DiningAreaId, table.Code, table.Capacity, now));
        return table;
    }

    /// <summary>Changes the table's seating capacity.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not positive.</exception>
    public void SetCapacity(int capacity)
    {
        RequirePositiveCapacity(capacity);
        if (Capacity == capacity) return;

        Capacity = capacity;
        AddDomainEvent(new TableCapacityChanged(Id, capacity, DateTimeOffset.UtcNow));
    }

    /// <summary>Seats the table - from <see cref="TableOccupancyStatus.Available"/> or <see cref="TableOccupancyStatus.Reserved"/>.</summary>
    /// <exception cref="RestaurantDomainException">The table is <see cref="TableOccupancyStatus.Occupied"/> or <see cref="TableOccupancyStatus.OutOfService"/>.</exception>
    public void Occupy()
    {
        if (OccupancyStatus is not (TableOccupancyStatus.Available or TableOccupancyStatus.Reserved))
            throw RestaurantDomainException.TableCannotBeOccupied(Id, OccupancyStatus);

        SetOccupancyStatus(TableOccupancyStatus.Occupied);
    }

    /// <summary>Frees the table - from <see cref="TableOccupancyStatus.Occupied"/> or <see cref="TableOccupancyStatus.Reserved"/>.</summary>
    /// <exception cref="RestaurantDomainException">The table is <see cref="TableOccupancyStatus.OutOfService"/>.</exception>
    public void Vacate()
    {
        if (OccupancyStatus == TableOccupancyStatus.OutOfService)
            throw RestaurantDomainException.TableCannotBeVacated(Id);

        SetOccupancyStatus(TableOccupancyStatus.Available);
    }

    /// <summary>Reserves the table for an upcoming party - from <see cref="TableOccupancyStatus.Available"/> only.</summary>
    /// <exception cref="RestaurantDomainException">The table is not <see cref="TableOccupancyStatus.Available"/>.</exception>
    public void Reserve()
    {
        if (OccupancyStatus != TableOccupancyStatus.Available)
            throw RestaurantDomainException.TableNotAvailable(Id);

        SetOccupancyStatus(TableOccupancyStatus.Reserved);
    }

    /// <summary>Takes the table out of service - from <see cref="TableOccupancyStatus.Available"/> only (a seated table must be vacated first).</summary>
    /// <exception cref="RestaurantDomainException">The table is not <see cref="TableOccupancyStatus.Available"/>.</exception>
    public void SetOutOfService()
    {
        if (OccupancyStatus != TableOccupancyStatus.Available)
            throw RestaurantDomainException.TableNotAvailable(Id);

        SetOccupancyStatus(TableOccupancyStatus.OutOfService);
    }

    /// <summary>Returns an out-of-service table to <see cref="TableOccupancyStatus.Available"/>.</summary>
    /// <exception cref="RestaurantDomainException">The table is not <see cref="TableOccupancyStatus.OutOfService"/>.</exception>
    public void ReturnToService()
    {
        if (OccupancyStatus != TableOccupancyStatus.OutOfService)
            throw RestaurantDomainException.TableNotOutOfService(Id);

        SetOccupancyStatus(TableOccupancyStatus.Available);
    }

    /// <summary>Activates the table (returns it to the floor plan).</summary>
    /// <exception cref="RestaurantDomainException">The table is already active.</exception>
    public void Activate()
    {
        if (Status == RestaurantStatus.Active)
            throw RestaurantDomainException.TableAlreadyActive(Id);

        Status = RestaurantStatus.Active;
        AddDomainEvent(new TableActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the table (removes it from the floor plan).</summary>
    /// <exception cref="RestaurantDomainException">The table is not active.</exception>
    public void Deactivate()
    {
        if (Status != RestaurantStatus.Active)
            throw RestaurantDomainException.TableNotActive(Id);

        Status = RestaurantStatus.Inactive;
        AddDomainEvent(new TableDeactivated(Id, DateTimeOffset.UtcNow));
    }

    private void SetOccupancyStatus(TableOccupancyStatus occupancyStatus)
    {
        if (OccupancyStatus == occupancyStatus) return;

        OccupancyStatus = occupancyStatus;
        AddDomainEvent(new TableOccupancyChanged(Id, occupancyStatus, DateTimeOffset.UtcNow));
    }

    private static void RequirePositiveCapacity(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
    }
}
