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
    decimal TotalInclusiveTax = 0m,
    Guid? OrganizationId = null,
    Guid? BranchId = null,
    Guid? TerminalId = null);

/// <summary>
/// Structural validation extensions for <see cref="ReceiptSnapshot"/>.
/// </summary>
public static class ReceiptSnapshotValidationExtensions
{
    /// <summary>
    /// Performs structural and mathematical validation on a deserialized receipt snapshot.
    /// Malformed or empty snapshots are rejected to prevent printing garbage or corrupted data.
    /// </summary>
    public static bool IsValid(this ReceiptSnapshot snapshot, out string? validationError)
    {
        if (snapshot.OrderId == Guid.Empty)
        {
            validationError = "OrderId is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(snapshot.OrderNumber))
        {
            validationError = "OrderNumber is empty or whitespace.";
            return false;
        }

        if (snapshot.Items == null || snapshot.Items.Count == 0)
        {
            validationError = "Receipt snapshot contains no line items.";
            return false;
        }

        for (int i = 0; i < snapshot.Items.Count; i++)
        {
            var item = snapshot.Items[i];
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                validationError = $"Item at index {i} has empty name.";
                return false;
            }
            if (item.Quantity <= 0)
            {
                validationError = $"Item '{item.Name}' has non-positive quantity ({item.Quantity}).";
                return false;
            }
            if (item.UnitPrice < 0)
            {
                validationError = $"Item '{item.Name}' has negative unit price ({item.UnitPrice}).";
                return false;
            }
            if (item.LineTotal < 0)
            {
                validationError = $"Item '{item.Name}' has negative line total ({item.LineTotal}).";
                return false;
            }
        }

        if (snapshot.Subtotal < 0)
        {
            validationError = $"Subtotal is negative ({snapshot.Subtotal}).";
            return false;
        }

        if (snapshot.GrandTotal < 0)
        {
            validationError = $"GrandTotal is negative ({snapshot.GrandTotal}).";
            return false;
        }

        if (snapshot.Payments == null)
        {
            validationError = "Payments collection is null.";
            return false;
        }

        validationError = null;
        return true;
    }
}
