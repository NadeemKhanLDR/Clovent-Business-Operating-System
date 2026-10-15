namespace Clovent.Restaurant.DomainServices;

/// <summary>
/// Result of an authoritative refund calculation for an order line item.
/// </summary>
public readonly record struct RefundLineCalculation(
    decimal GrossRefund,
    decimal DiscountReversed,
    decimal TaxReversed,
    decimal NetPayableRefund);

/// <summary>
/// Authoritative calculation engine for order line refunds.
/// Governs proportional reversal of allocated discounts, applicable sales taxes,
/// and exact absorption of residual rounding on final partial refunds.
/// </summary>
public static class RefundCalculator
{
    /// <summary>
    /// Calculates the refund breakdown for a line item according to the centralized financial policy.
    /// </summary>
    public static RefundLineCalculation CalculateLineRefund(
        decimal originalQuantity,
        decimal unitPrice,
        decimal allocatedDiscount,
        decimal taxAmount,
        bool taxIsInclusive,
        decimal priorRefundedQuantity,
        decimal priorDiscountReversed,
        decimal priorTaxReversed,
        decimal requestedQuantity)
    {
        if (requestedQuantity <= 0m || originalQuantity <= 0m)
        {
            return new RefundLineCalculation(0m, 0m, 0m, 0m);
        }

        var remainingQuantity = Math.Max(0m, originalQuantity - priorRefundedQuantity);
        if (remainingQuantity <= 0m)
        {
            return new RefundLineCalculation(0m, 0m, 0m, 0m);
        }

        var effectiveQuantity = Math.Min(requestedQuantity, remainingQuantity);

        decimal discountReversed;
        decimal taxReversed;

        if (effectiveQuantity == remainingQuantity)
        {
            // Final partial or full refund of the remaining line: absorbs exact residual cents
            discountReversed = Math.Max(0m, allocatedDiscount - priorDiscountReversed);
            taxReversed = Math.Max(0m, taxAmount - priorTaxReversed);
        }
        else
        {
            // Intermediate partial refund: proportional allocation rounded AwayFromZero
            var ratio = effectiveQuantity / originalQuantity;
            discountReversed = MoneyRoundingPolicy.RoundMoney(allocatedDiscount * ratio);
            taxReversed = MoneyRoundingPolicy.RoundMoney(taxAmount * ratio);
        }

        var grossRefund = MoneyRoundingPolicy.RoundMoney(effectiveQuantity * unitPrice);
        var netPayableRefund = grossRefund - discountReversed + (taxIsInclusive ? 0m : taxReversed);

        return new RefundLineCalculation(grossRefund, discountReversed, taxReversed, netPayableRefund);
    }
}
