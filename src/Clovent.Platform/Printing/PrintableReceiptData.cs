namespace Clovent.Platform.Printing;

/// <summary>Single line item in a printable receipt data contract.</summary>
public sealed record PrintableReceiptItem(
    string Name,
    string? Sku,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? Notes = null);

/// <summary>Single payment tender in a printable receipt data contract.</summary>
public sealed record PrintableReceiptPayment(
    string PaymentMethodName,
    decimal Amount);

/// <summary>Single tax breakdown entry in a printable receipt data contract.</summary>
public sealed record PrintableReceiptTax(
    string TaxName,
    decimal TaxRate,
    decimal TaxAmount);

/// <summary>
/// General-purpose, immutable data contract for receipt document rendering.
/// Decouples platform printing and hardware adapters from vertical-specific domain models.
/// </summary>
public sealed record PrintableReceiptData(
    Guid OrderId,
    string OrderNumber,
    int? DailySalesNumber,
    string OrderType,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<PrintableReceiptItem> Items,
    decimal Subtotal,
    decimal TaxTotal,
    decimal DiscountTotal,
    decimal ServiceChargeTotal,
    decimal GrandTotal,
    decimal Balance,
    IReadOnlyList<PrintableReceiptPayment> Payments,
    string? CashierName = null,
    string? TerminalName = null,
    string? BranchName = null,
    string? OrganizationName = null,
    string? TaxRegistrationNumber = null,
    string? CustomerNotes = null,
    IReadOnlyList<PrintableReceiptTax>? TaxBreakdown = null);
