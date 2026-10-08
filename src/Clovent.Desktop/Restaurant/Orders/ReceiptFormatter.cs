using System.Text;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.Discounts.Queries;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Application.Payments.Queries;
using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Application.ServiceCharges.Queries;
using MediatR;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Builds plain-text customer receipts, reprints, and refund credit notes.
/// Consumes immutable snapshots on reprint without altering historical facts.
/// Formats statutory sales tax presentation distinguishing taxable, zero-rated, exempt, and out-of-scope items.
/// </summary>
public static class ReceiptFormatter
{
    /// <summary>Formats <paramref name="order"/> into a plain-text receipt.</summary>
    public static async Task<string> FormatAsync(IMediator mediator, OrderDto order)
    {
        if (!string.IsNullOrWhiteSpace(order.ReceiptSnapshotJson))
        {
            try
            {
                var snapshot = System.Text.Json.JsonSerializer.Deserialize<Clovent.Restaurant.Orders.ReceiptSnapshot>(order.ReceiptSnapshotJson);
                if (snapshot != null)
                {
                    return FormatFromSnapshot(snapshot);
                }
            }
            catch
            {
                // Fallback to dynamic query if snapshot parsing fails
            }
        }

        var lines = await mediator.Send(new ListOrderLinesByOrderQuery(order.OrderId));
        var discounts = await mediator.Send(new ListDiscountsByOrderQuery(order.OrderId));
        var serviceCharges = await mediator.Send(new ListServiceChargesByOrderQuery(order.OrderId));
        var payments = await mediator.Send(new ListPaymentsByOrderQuery(order.OrderId));

        var activeLines = lines.Where(l => !l.IsVoided).ToList();
        var totals = OrderTotalsCalculator.Calculate(lines, discounts, serviceCharges, payments);

        var sb = new StringBuilder();
        sb.AppendLine("Clovent Business Operating System");
        sb.AppendLine($"Order: {order.OrderNumber}");
        if (order.DailySalesNumber is { } dailySalesNumber)
        {
            sb.AppendLine($"Sale #: {dailySalesNumber}");
        }
        sb.AppendLine($"Type: {order.OrderType}");
        sb.AppendLine(new string('-', 40));

        foreach (var line in activeLines)
        {
            var name = await ResolveVariantNameAsync(mediator, line.ProductVariantId);
            var taxTag = FormatTaxTag(line.TaxClassification, line.TaxRatePercentage, line.TaxIsInclusive);
            sb.AppendLine($"{name} x{line.Quantity:N2} @ {CurrencyDisplay.Format(line.UnitPrice)} = {CurrencyDisplay.Format(line.LineTotal)} {taxTag}".TrimEnd());
            if (line.Notes is { } notes)
            {
                sb.AppendLine($"  Note: {notes}");
            }
        }

        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"Subtotal: {CurrencyDisplay.Format(totals.Subtotal)}");
        AppendIfNonZero(sb, "Discounts", -totals.DiscountTotal);
        if (totals.TaxableBaseTotal > 0 && totals.TaxableBaseTotal != totals.Subtotal)
        {
            sb.AppendLine($"Net Taxable Sales: {CurrencyDisplay.Format(totals.TaxableBaseTotal)}");
        }
        AppendIfNonZero(sb, "Service Charge", totals.ServiceChargeTotal);
        if (totals.ExclusiveTaxTotal > 0m)
        {
            sb.AppendLine($"Exclusive Tax: {CurrencyDisplay.Format(totals.ExclusiveTaxTotal)}");
        }
        if (totals.InclusiveTaxTotal > 0m)
        {
            sb.AppendLine($"Included Tax (in prices): {CurrencyDisplay.Format(totals.InclusiveTaxTotal)}");
        }
        sb.AppendLine($"Grand Total: {CurrencyDisplay.Format(totals.GrandTotal)}");

        if (totals.TaxSummary is { Count: > 0 } summary)
        {
            sb.AppendLine(new string('-', 40));
            sb.AppendLine("Tax Summary:");
            foreach (var item in summary)
            {
                sb.AppendLine($"  {item.TaxCode} ({item.RatePercentage:0.##}%): Base {CurrencyDisplay.Format(item.TaxableBase)} | Tax {CurrencyDisplay.Format(item.TaxAmount)}");
            }
        }

        sb.AppendLine(new string('-', 40));

        foreach (var payment in payments.Where(p => !p.IsVoided))
        {
            sb.AppendLine($"Payment: {CurrencyDisplay.Format(payment.Amount)}");
        }

        sb.AppendLine($"Balance: {CurrencyDisplay.Format(totals.Balance)}");

        if (order.CustomerNotes is { } customerNotes)
        {
            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"Notes: {customerNotes}");
        }

        return sb.ToString();
    }

    /// <summary>Formats a plain-text receipt directly from a durable snapshot.</summary>
    public static string FormatFromSnapshot(Clovent.Restaurant.Orders.ReceiptSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Clovent Business Operating System");
        sb.AppendLine($"Order: {snapshot.OrderNumber}");
        if (snapshot.DailySalesNumber is { } dailySalesNumber)
        {
            sb.AppendLine($"Sale #: {dailySalesNumber}");
        }
        sb.AppendLine($"Type: {snapshot.OrderType}");
        if (!string.IsNullOrWhiteSpace(snapshot.TerminalName))
        {
            sb.AppendLine($"Terminal: {snapshot.TerminalName}");
        }
        if (!string.IsNullOrWhiteSpace(snapshot.CashierName))
        {
            sb.AppendLine($"Cashier: {snapshot.CashierName}");
        }
        sb.AppendLine(new string('-', 40));

        foreach (var item in snapshot.Items)
        {
            var name = !string.IsNullOrWhiteSpace(item.Name) ? item.Name : (!string.IsNullOrWhiteSpace(item.Sku) ? item.Sku : "Item");
            var taxTag = FormatTaxTag(item.TaxClassification, item.TaxRatePercentage, item.TaxIsInclusive);
            sb.AppendLine($"{name} x{item.Quantity:N2} @ {CurrencyDisplay.Format(item.UnitPrice)} = {CurrencyDisplay.Format(item.LineTotal)} {taxTag}".TrimEnd());
            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                sb.AppendLine($"  Note: {item.Notes}");
            }
        }

        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"Subtotal: {CurrencyDisplay.Format(snapshot.Subtotal)}");
        AppendIfNonZero(sb, "Discounts", -snapshot.DiscountTotal);
        if (snapshot.TotalTaxableBase > 0 && snapshot.TotalTaxableBase != snapshot.Subtotal)
        {
            sb.AppendLine($"Net Taxable Sales: {CurrencyDisplay.Format(snapshot.TotalTaxableBase)}");
        }
        AppendIfNonZero(sb, "Service Charge", snapshot.ServiceChargeTotal);
        if (snapshot.TotalExclusiveTax > 0m)
        {
            sb.AppendLine($"Exclusive Tax: {CurrencyDisplay.Format(snapshot.TotalExclusiveTax)}");
        }
        else if (snapshot.TaxTotal > 0m && snapshot.TotalInclusiveTax == 0m)
        {
            sb.AppendLine($"Tax: {CurrencyDisplay.Format(snapshot.TaxTotal)}");
        }

        if (snapshot.TotalInclusiveTax > 0m)
        {
            sb.AppendLine($"Included Tax (in prices): {CurrencyDisplay.Format(snapshot.TotalInclusiveTax)}");
        }

        sb.AppendLine($"Grand Total: {CurrencyDisplay.Format(snapshot.GrandTotal)}");

        if (snapshot.TaxBreakdown is { Count: > 0 } breakdown)
        {
            sb.AppendLine(new string('-', 40));
            sb.AppendLine("Tax Summary:");
            foreach (var item in breakdown)
            {
                var tag = item.TaxClassification == "Exempt" ? "Exempt" : (item.TaxClassification == "ZeroRated" ? "0% Zero-Rated" : $"{item.RatePercentage:0.##}%");
                sb.AppendLine($"  {item.TaxCode} ({tag}): Base {CurrencyDisplay.Format(item.TaxableBase)} | Tax {CurrencyDisplay.Format(item.TaxAmount)}");
            }
        }
        else if (snapshot.TaxTotal > 0m && snapshot.TotalExclusiveTax == 0m && snapshot.TotalInclusiveTax == 0m)
        {
            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"Tax Total: {CurrencyDisplay.Format(snapshot.TaxTotal)}");
        }

        sb.AppendLine(new string('-', 40));

        foreach (var payment in snapshot.Payments)
        {
            sb.AppendLine($"Payment ({payment.PaymentMethodName}): {CurrencyDisplay.Format(payment.Amount)}");
        }

        sb.AppendLine($"Balance: {CurrencyDisplay.Format(snapshot.Balance)}");

        if (!string.IsNullOrWhiteSpace(snapshot.CustomerNotes))
        {
            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"Notes: {snapshot.CustomerNotes}");
        }

        return sb.ToString();
    }

    /// <summary>Formats a plain-text refund credit note referencing the original invoice.</summary>
    public static string FormatRefundCreditNote(
        string refundNumber,
        string originalOrderNumber,
        DateTimeOffset refundedAtUtc,
        string cashierName,
        string? approvedByUserName,
        string reason,
        string settlementMethod,
        IReadOnlyList<RefundItemPrintLine> items,
        decimal subtotalRefunded,
        decimal discountReversed,
        decimal taxReversed,
        decimal grandTotalRefunded)
    {
        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("       CREDIT NOTE / REFUND RECEIPT");
        sb.AppendLine("   Clovent Business Operating System");
        sb.AppendLine("========================================");
        sb.AppendLine($"Credit Note #: {refundNumber}");
        sb.AppendLine($"Original Invoice: {originalOrderNumber}");
        sb.AppendLine($"Date: {refundedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Cashier: {cashierName}");
        if (!string.IsNullOrWhiteSpace(approvedByUserName))
        {
            sb.AppendLine($"Approved By: {approvedByUserName}");
        }
        sb.AppendLine($"Reason: {reason}");
        sb.AppendLine($"Settlement: {settlementMethod}");
        sb.AppendLine(new string('-', 40));

        foreach (var item in items)
        {
            sb.AppendLine($"{item.ItemName} x{item.Quantity:N2} @ {CurrencyDisplay.Format(item.UnitPrice)} = {CurrencyDisplay.Format(item.LineTotal)}");
            sb.AppendLine($"  Disposition: {item.InventoryDisposition} | Tax Reversed: {CurrencyDisplay.Format(item.TaxReversed)}");
        }

        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"Gross Value Refunded: {CurrencyDisplay.Format(subtotalRefunded)}");
        if (discountReversed > 0m)
        {
            sb.AppendLine($"Discount Reversed: -{CurrencyDisplay.Format(discountReversed)}");
        }
        if (taxReversed > 0m)
        {
            sb.AppendLine($"Tax Reversed: {CurrencyDisplay.Format(taxReversed)}");
        }
        sb.AppendLine($"Total Refund Payable: {CurrencyDisplay.Format(grandTotalRefunded)}");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine("Goods returned in satisfactory condition.");
        sb.AppendLine("Customer Signature: ____________________");
        return sb.ToString();
    }

    private static string FormatTaxTag(string? classification, decimal rate, bool isInclusive)
    {
        if (string.Equals(classification, "Exempt", StringComparison.OrdinalIgnoreCase)) return "[E]";
        if (string.Equals(classification, "ZeroRated", StringComparison.OrdinalIgnoreCase)) return "[Z]";
        if (string.Equals(classification, "OutOfScope", StringComparison.OrdinalIgnoreCase)) return "[N]";
        if (rate > 0m) return $"[T: {rate:0.##}% {(isInclusive ? "Inc" : "Exc")}]";
        return "";
    }

    private static void AppendIfNonZero(StringBuilder sb, string label, decimal amount)
    {
        if (amount != 0m)
        {
            sb.AppendLine($"{label}: {CurrencyDisplay.Format(amount)}");
        }
    }

    private static async Task<string> ResolveVariantNameAsync(IMediator mediator, Guid productVariantId)
    {
        var variant = await mediator.Send(new GetProductVariantByIdQuery(productVariantId));
        return variant.Name;
    }
}

/// <summary>Print item model for refund receipts.</summary>
public sealed record RefundItemPrintLine(
    string ItemName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal DiscountReversed,
    decimal TaxReversed,
    string InventoryDisposition);
