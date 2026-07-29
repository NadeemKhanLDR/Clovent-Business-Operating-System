namespace Clovent.Inventory.Adjustments;

/// <summary>A <see cref="StockAdjustment"/>'s workflow state.</summary>
public enum StockAdjustmentStatus
{
    /// <summary>Proposed but not yet applied to warehouse stock.</summary>
    Pending,

    /// <summary>Applied to warehouse stock - a one-way transition, no undo.</summary>
    Applied,

    /// <summary>Cancelled before being applied.</summary>
    Cancelled
}
