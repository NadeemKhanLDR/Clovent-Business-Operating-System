using Clovent.Platform.Printing;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Printing;

/// <summary>
/// Maps restaurant domain <see cref="ReceiptSnapshot"/> instances into
/// general-purpose <see cref="PrintableReceiptData"/> contracts for platform printing.
/// </summary>
public static class ReceiptSnapshotMappingExtensions
{
    /// <summary>
    /// Converts a <see cref="ReceiptSnapshot"/> into a <see cref="PrintableReceiptData"/> contract.
    /// Preserves immutable sale-time figures without recalculating taxes or totals.
    /// </summary>
    public static PrintableReceiptData ToPrintable(
        this ReceiptSnapshot snapshot,
        string? branchName = null,
        string? organizationName = null,
        string? taxRegistrationNumber = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var items = snapshot.Items.Select(i => new PrintableReceiptItem(
            Name: i.Name,
            Sku: i.Sku,
            Quantity: i.Quantity,
            UnitPrice: i.UnitPrice,
            LineTotal: i.LineTotal,
            Notes: i.Notes)).ToList();

        var payments = snapshot.Payments.Select(p => new PrintableReceiptPayment(
            PaymentMethodName: p.PaymentMethodName,
            Amount: p.Amount)).ToList();

        return new PrintableReceiptData(
            OrderId: snapshot.OrderId,
            OrderNumber: snapshot.OrderNumber,
            DailySalesNumber: snapshot.DailySalesNumber,
            OrderType: snapshot.OrderType,
            CompletedAtUtc: snapshot.CompletedAtUtc,
            Items: items,
            Subtotal: snapshot.Subtotal,
            TaxTotal: snapshot.TaxTotal,
            DiscountTotal: snapshot.DiscountTotal,
            ServiceChargeTotal: snapshot.ServiceChargeTotal,
            GrandTotal: snapshot.GrandTotal,
            Balance: snapshot.Balance,
            Payments: payments,
            CashierName: snapshot.CashierName,
            TerminalName: snapshot.TerminalName,
            BranchName: branchName,
            OrganizationName: organizationName,
            TaxRegistrationNumber: taxRegistrationNumber,
            CustomerNotes: snapshot.CustomerNotes);
    }
}
