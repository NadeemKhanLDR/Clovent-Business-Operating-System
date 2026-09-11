namespace Clovent.Restaurant.Shifts;

/// <summary>Represents the lifecycle state of a cash register <see cref="Shift"/> session.</summary>
public enum ShiftStatus
{
    /// <summary>The shift is active and actively recording sales and cash movements.</summary>
    Open = 0,

    /// <summary>The shift has been reconciled and closed.</summary>
    Closed = 1,

    /// <summary>The shift was cancelled or voided before completion.</summary>
    Cancelled = 2
}
