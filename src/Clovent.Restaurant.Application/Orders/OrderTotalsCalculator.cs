using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.ServiceCharges;

namespace Clovent.Restaurant.Application.Orders;

/// <summary>
/// Pure calculation logic for an order's running total, tax summary, and
/// payment balance - deliberately kept out of the <c>Order</c> aggregate
/// (see its doc comment) and out of any command handler, so it can be unit
/// tested without a repository or a database. Used by both
/// <c>CompleteOrderCommandHandler</c> (to verify the balance is zero before
/// completing) and <c>GetOrderSummaryQuery</c> (to show the POS screen's
/// running total).
/// </summary>
public static class OrderTotalsCalculator
{
    /// <summary>Computes every figure in <see cref="OrderTotals"/> from an order's lines, discounts, service charges, and payments.</summary>
    public static OrderTotals Calculate(
        IReadOnlyCollection<OrderLineDto> lines,
        IReadOnlyCollection<DiscountDto> discounts,
        IReadOnlyCollection<ServiceChargeDto> serviceCharges,
        IReadOnlyCollection<PaymentDto> payments)
    {
        var activeLines = lines.Where(l => !l.IsVoided).ToList();

        var subtotal = activeLines.Sum(l => l.Quantity * l.UnitPrice);
        var taxTotal = activeLines.Sum(CalculateLineTax);
        var exclusiveTaxAddOn = activeLines.Where(l => !l.TaxIsInclusive).Sum(CalculateLineTax);

        var discountTotal = discounts.Sum(d => ResolveAmount(d.DiscountType == nameof(DiscountType.Percentage), d.Value, subtotal));
        var serviceChargeTotal = serviceCharges.Sum(s => ResolveAmount(s.ServiceChargeType == nameof(ServiceChargeType.Percentage), s.Value, subtotal));

        var grandTotal = subtotal - discountTotal + serviceChargeTotal + exclusiveTaxAddOn;
        var paidTotal = payments.Where(p => !p.IsVoided).Sum(p => p.Amount);
        var balance = grandTotal - paidTotal;

        return new OrderTotals(subtotal, taxTotal, discountTotal, serviceChargeTotal, grandTotal, paidTotal, balance);
    }

    private static decimal CalculateLineTax(OrderLineDto line)
    {
        if (line.TaxRatePercentage <= 0) return 0m;

        var lineTotal = line.Quantity * line.UnitPrice;
        return line.TaxIsInclusive
            ? lineTotal - lineTotal / (1 + line.TaxRatePercentage / 100m)
            : lineTotal * line.TaxRatePercentage / 100m;
    }

    private static decimal ResolveAmount(bool isPercentage, decimal value, decimal subtotal) =>
        isPercentage ? subtotal * value / 100m : value;
}
