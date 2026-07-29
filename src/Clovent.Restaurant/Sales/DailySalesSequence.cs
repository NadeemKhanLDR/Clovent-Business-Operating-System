using Clovent.Domain;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Sales.Events;

namespace Clovent.Restaurant.Sales;

/// <summary>
/// A per-warehouse, per-day counter backing the "Daily Sales Number" - a
/// small, human-friendly sequential number reset each day, distinct from
/// <see cref="Orders.ValueObjects.OrderNumber"/> (globally unique,
/// time-derived, assigned at order creation). One row exists per
/// (<see cref="WarehouseId"/>, <see cref="Date"/>) pair, created on first use
/// by whichever order completes first that day at that warehouse.
/// </summary>
public sealed class DailySalesSequence : AggregateRoot<DailySalesSequenceId>
{
    /// <summary>The warehouse this sequence counts sales for.</summary>
    public WarehouseId WarehouseId { get; }

    /// <summary>The calendar day (UTC) this sequence counts sales for.</summary>
    public DateOnly Date { get; }

    /// <summary>The last number issued so far.</summary>
    public int LastNumber { get; private set; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private DailySalesSequence(DailySalesSequenceId id, WarehouseId warehouseId, DateOnly date, int lastNumber)
    {
        Id = id;
        WarehouseId = warehouseId;
        Date = date;
        LastNumber = lastNumber;
    }

    /// <summary>Creates a new sequence for a warehouse/date pair, starting at zero (no number issued yet).</summary>
    public static DailySalesSequence Create(WarehouseId warehouseId, DateOnly date) =>
        new(DailySalesSequenceId.New(), warehouseId, date, lastNumber: 0);

    /// <summary>Issues and returns the next number in the sequence.</summary>
    public int Next()
    {
        LastNumber++;
        AddDomainEvent(new DailySalesSequenceAdvanced(Id, WarehouseId, Date, LastNumber, DateTimeOffset.UtcNow));
        return LastNumber;
    }
}
