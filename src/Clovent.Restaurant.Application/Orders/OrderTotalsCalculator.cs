using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;

namespace Clovent.Restaurant.Application.Orders;

/// <summary>
/// Pure calculation logic for an order's running total, tax summary, and
/// payment balance. Governed by the centralized <see cref="MoneyRoundingPolicy"/>
/// and <see cref="TaxCalculator"/> adhering to the AwayFromZero midpoint rounding rule.
/// Used by <c>CompleteOrderCommandHandler</c>, <c>GetOrderSummaryQuery</c>,
/// receipt formatting, and Day Close summaries.
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
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(discounts);
        ArgumentNullException.ThrowIfNull(serviceCharges);
        ArgumentNullException.ThrowIfNull(payments);

        var activeLines = lines.Where(l => !l.IsVoided).ToList();

        // 1. Compute gross line base
        var subtotal = activeLines.Sum(l => MoneyRoundingPolicy.RoundMoney(l.Quantity * l.UnitPrice));

        // 2. Resolve order-level discount amount
        var orderDiscountTotal = discounts.Sum(d =>
            ResolveAmount(d.DiscountType == nameof(DiscountType.Percentage), d.Value, subtotal));

        // 3. Prepare inputs for TaxCalculator
        var calcLines = activeLines.Select(l => new TaxCalculationLineInput(
            l.Id,
            l.Quantity,
            l.UnitPrice,
            0m,
            l.TaxRatePercentage > 0m ? "Taxable" : "Exempt",
            "PRA",
            $"PK-TAX-{l.TaxRatePercentage:0.##}",
            l.TaxRatePercentage,
            l.TaxIsInclusive)).ToList();

        var taxResult = TaxCalculator.Calculate(calcLines, orderDiscountTotal);
        var totalDiscounts = orderDiscountTotal + taxResult.TotalLineDiscounts;

        // 4. Resolve service charges
        var serviceChargeTotal = serviceCharges.Sum(s =>
            ResolveAmount(s.ServiceChargeType == nameof(ServiceChargeType.Percentage), s.Value, subtotal));

        // 5. Compute Grand Total and Balance
        var grandTotal = MoneyRoundingPolicy.RoundMoney(
            subtotal - totalDiscounts + serviceChargeTotal + taxResult.TotalExclusiveTax);

        var paidTotal = payments.Where(p => !p.IsVoided).Sum(p => MoneyRoundingPolicy.RoundMoney(p.Amount));
        var balance = MoneyRoundingPolicy.RoundMoney(grandTotal - paidTotal);

        return new OrderTotals(
            subtotal,
            taxResult.TotalTax,
            totalDiscounts,
            serviceChargeTotal,
            grandTotal,
            paidTotal,
            balance,
            taxResult.TotalExclusiveTax,
            taxResult.TotalInclusiveTax,
            taxResult.TotalTaxableBase,
            taxResult.LineSnapshots,
            taxResult.TaxSummary);
    }

    private static decimal ResolveAmount(bool isPercentage, decimal value, decimal subtotal) =>
        isPercentage
            ? MoneyRoundingPolicy.RoundMoney(subtotal * value / 100m)
            : MoneyRoundingPolicy.RoundMoney(value);
}
