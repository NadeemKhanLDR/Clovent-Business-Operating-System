namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Which orders the Order History screen shows, and how a row is searched -
/// kept apart from <see cref="OrderHistoryView"/> so the screen and its tests
/// exercise the same rules rather than a re-implementation, the same split
/// <see cref="PosPaymentRules"/> already uses for the tender strip.
/// </summary>
public static class OrderHistoryRules
{
    /// <summary>
    /// The statuses this screen lists: the closed ones. Open and Held are
    /// deliberately excluded - they already have their own screens (Running
    /// Orders and Held Orders) and remain editable in
    /// <see cref="RestaurantPosForm"/>, so listing them here would create a
    /// second, read-only path to a live order.
    /// </summary>
    public static readonly string[] ClosedStatuses = ["Completed", "Cancelled", "Voided"];

    /// <summary>Whether an order belongs on the Order History screen.</summary>
    public static bool IsClosed(string? status) => status is not null && ClosedStatuses.Contains(status);

    /// <summary>
    /// The free-text haystack for one row. Order number and table code are the
    /// two things a cashier actually has to hand when chasing a bill; status is
    /// included so "cancelled" narrows the list without a separate filter
    /// control.
    /// </summary>
    public static string SearchText(string orderNumber, string tableCode, string status, int? dailySalesNumber) =>
        $"{orderNumber} {tableCode} {status} {dailySalesNumber}";
}
