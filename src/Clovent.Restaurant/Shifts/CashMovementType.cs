namespace Clovent.Restaurant.Shifts;

/// <summary>Indicates whether a cash movement adds cash to or removes cash from the register drawer.</summary>
public enum CashMovementType
{
    /// <summary>Cash added to the drawer (e.g. float top-up, misc receipt).</summary>
    CashIn = 0,

    /// <summary>Cash removed from the drawer (e.g. paid-out expense, safe drop, withdrawal).</summary>
    CashOut = 1
}
