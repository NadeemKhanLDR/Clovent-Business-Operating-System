namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Encodes the "table selection is Dine-In intent" business rule shared by
/// the POS table picker flow: selecting a valid restaurant table with no
/// working order must immediately start a Dine-In working order for that
/// table, with no extra "+ Dine In" click. Selecting "(No Table)" must not
/// create an order, and an existing working order must never be duplicated
/// merely because the table dropdown changed.
/// </summary>
internal static class TableSelectionDineInPolicy
{
    public static bool ShouldAutoStartDineIn(bool hasWorkingOrder, Guid? selectedTableId)
        => !hasWorkingOrder && selectedTableId is not null;

    /// <summary>
    /// Finds the first active and available table from the table collection according to natural enumeration order.
    /// </summary>
    public static TTable? FindFirstAvailableTable<TTable>(IEnumerable<TTable> tables, Func<TTable, string> statusSelector, Func<TTable, string> occupancyStatusSelector)
        => tables.FirstOrDefault(t =>
            string.Equals(statusSelector(t), "Active", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(occupancyStatusSelector(t), "Available", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Determines whether an item tap when no active order exists should trigger auto-selection of a Dine-In table.
    /// When the POS is currently in Take Away mode, auto-table selection is skipped.
    /// </summary>
    public static bool ShouldAutoSelectTableOnItemTap(bool hasWorkingOrder, string? activeOrdersFilter)
        => !hasWorkingOrder && !string.Equals(activeOrdersFilter, "TakeAway", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether clearing the current working order should cancel it as an unused draft.
    /// An empty Dine-In order with 0 lines and 0 payments is cancelled to vacate its table.
    /// Non-DineIn orders or orders with lines/payments are never automatically cancelled.
    /// </summary>
    public static bool ShouldCancelEmptyDraftOnClear(string? orderType, int lineCount, int paymentCount = 0)
        => string.Equals(orderType, "DineIn", StringComparison.OrdinalIgnoreCase) && lineCount == 0 && paymentCount == 0;
}
