using Clovent.Restaurant.Orders;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// UI policy helper governing payment-void availability in <see cref="PaymentHistoryDialog"/>
/// so completed financial transactions remain immutable in the POS and Order History UIs.
/// </summary>
public static class PaymentHistoryRules
{
    /// <summary>Explanation surfaced to cashiers/managers when viewing payments on a completed order.</summary>
    public const string CompletedOrderVoidBlockedMessage =
        "Completed orders are immutable. Payments cannot be voided; use Order History -> Refund Order to issue a compensating refund.";

    /// <summary>Returns whether Void Payment should be enabled for <paramref name="orderStatus"/> and an optional explanation message.</summary>
    public static (bool CanVoid, string? Explanation) EvaluateVoidAvailability(OrderStatus orderStatus) =>
        orderStatus == OrderStatus.Completed
            ? (false, CompletedOrderVoidBlockedMessage)
            : (true, null);

    /// <summary>String-status overload matching <c>OrderDto.Status</c>.</summary>
    public static (bool CanVoid, string? Explanation) EvaluateVoidAvailability(string? orderStatus) =>
        string.Equals(orderStatus, nameof(OrderStatus.Completed), StringComparison.OrdinalIgnoreCase)
            ? (false, CompletedOrderVoidBlockedMessage)
            : (true, null);
}
