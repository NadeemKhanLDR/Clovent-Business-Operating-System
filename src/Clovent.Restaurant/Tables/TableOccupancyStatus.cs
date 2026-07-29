namespace Clovent.Restaurant.Tables;

/// <summary>
/// A <see cref="Table"/>'s real-time floor state - distinct from its
/// <see cref="Clovent.Restaurant.Shared.RestaurantStatus"/> lifecycle
/// (whether the table exists on the floor plan at all). A table can be
/// <c>Active</c>-and-<see cref="Available"/> one moment and
/// <c>Active</c>-and-<see cref="Occupied"/> the next without ever
/// touching its lifecycle status.
/// </summary>
public enum TableOccupancyStatus
{
    /// <summary>Free and ready to seat.</summary>
    Available,

    /// <summary>Currently seated with an open order.</summary>
    Occupied,

    /// <summary>Held for an upcoming party, not yet seated.</summary>
    Reserved,

    /// <summary>Not available for seating (e.g. under repair).</summary>
    OutOfService
}
