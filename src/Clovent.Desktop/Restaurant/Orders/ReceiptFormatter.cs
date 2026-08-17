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
/// Builds a plain-text receipt preview for an order - resolves each line's
/// product name via <c>Clovent.Catalog.Application</c>'s existing variant
/// query rather than duplicating catalog data on the order line, and reuses
/// <see cref="OrderTotalsCalculator"/> for every money figure so the receipt
/// always agrees with the POS screen's running total.
/// </summary>
public static class ReceiptFormatter
{
    /// <summary>Formats <paramref name="order"/> into a plain-text receipt.</summary>
    public static async Task<string> FormatAsync(IMediator mediator, OrderDto order)
    {
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
            sb.AppendLine($"{name} x{line.Quantity:N2} @ {CurrencyDisplay.Format(line.UnitPrice)} = {CurrencyDisplay.Format(line.LineTotal)}");
            if (line.Notes is { } notes)
            {
                sb.AppendLine($"  Note: {notes}");
            }
        }

        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"Subtotal: {CurrencyDisplay.Format(totals.Subtotal)}");
        sb.AppendLine($"Tax: {CurrencyDisplay.Format(totals.TaxTotal)}");
        AppendIfNonZero(sb, "Discount", -totals.DiscountTotal);
        AppendIfNonZero(sb, "Service Charge", totals.ServiceChargeTotal);
        sb.AppendLine($"Grand Total: {CurrencyDisplay.Format(totals.GrandTotal)}");
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
