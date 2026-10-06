namespace Clovent.Restaurant.Orders;

/// <summary>Single immutable line item in a completed receipt snapshot.</summary>
public sealed record ReceiptSnapshotItem(
    Guid ProductVariantId,
    string Sku,
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? Notes);

/// <summary>Single immutable payment tender in a completed receipt snapshot.</summary>
public sealed record ReceiptSnapshotPayment(
    decimal Amount,
    string PaymentMethodName);

/// <summary>
/// Frozen immutable snapshot of an order receipt captured at transaction completion.
/// Guaranteed to reproduce the customer receipt identically on reprint without
/// querying catalog variants, current prices, or external services.
/// </summary>
public sealed record ReceiptSnapshot(
    Guid OrderId,
    string OrderNumber,
    int? DailySalesNumber,
    string OrderType,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<ReceiptSnapshotItem> Items,
    decimal Subtotal,
    decimal TaxTotal,
    decimal DiscountTotal,
    decimal ServiceChargeTotal,
    decimal GrandTotal,
    decimal Balance,
    IReadOnlyList<ReceiptSnapshotPayment> Payments,
    string? CashierName,
    string? TerminalName,
    string? CustomerNotes);
