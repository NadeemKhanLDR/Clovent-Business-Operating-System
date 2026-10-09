namespace Clovent.Restaurant.Orders;

/// <summary>Single immutable line item in a completed receipt snapshot.</summary>
public sealed record ReceiptSnapshotItem(
    Guid ProductVariantId,
    string Sku,
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? Notes,
    string TaxClassification = "Taxable",
    string TaxCode = "PK-PRA-16",
    decimal TaxRatePercentage = 0m,
    bool TaxIsInclusive = false,
    decimal TaxableBase = 0m,
    decimal TaxAmount = 0m,
    decimal DiscountAmount = 0m);

/// <summary>Single immutable payment tender in a completed receipt snapshot.</summary>
public sealed record ReceiptSnapshotPayment(
    decimal Amount,
    string PaymentMethodName);

/// <summary>
/// Frozen immutable snapshot of an order receipt captured at transaction completion.
/// Guaranteed to reproduce the customer receipt identically on reprint without
/// querying catalog variants, current prices, or external services.
/// Preserves statutory tax breakdown facts and calculation policy version.
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
    string? CustomerNotes,
    IReadOnlyList<OrderTaxSummarySnapshot>? TaxBreakdown = null,
    string? CalculationPolicyVersion = "1.3.0-AwayFromZero-v1",
    decimal TotalTaxableBase = 0m,
    decimal TotalExclusiveTax = 0m,
    decimal TotalInclusiveTax = 0m);
