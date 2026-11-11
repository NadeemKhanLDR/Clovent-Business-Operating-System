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
        decimal requestedQuantity) =>
        CalculateLineRefund(
            originalQuantity,
            unitPrice,
            allocatedDiscount,
            taxAmount,
            taxIsInclusive,
            priorRefundedQuantity,
            priorDiscountReversed,
            priorTaxReversed,
            requestedQuantity,
            priorGrossRefunded: null);

    /// <summary>
    /// Calculates the refund breakdown for a line item using explicit cumulative prior gross refunded
    /// so repeating fractional refunds absorb exact rounding residuals across gross, discount, tax, and net totals.
    /// Rejects negative parameters, inconsistent historical state, or requested quantities exceeding remaining eligibility.
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
        decimal requestedQuantity,
        decimal? priorGrossRefunded)
    {
        if (originalQuantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(originalQuantity), originalQuantity, "Original quantity cannot be negative.");
        if (unitPrice < 0m)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Unit price cannot be negative.");
        if (allocatedDiscount < 0m)
            throw new ArgumentOutOfRangeException(nameof(allocatedDiscount), allocatedDiscount, "Allocated discount cannot be negative.");
        if (taxAmount < 0m)
            throw new ArgumentOutOfRangeException(nameof(taxAmount), taxAmount, "Tax amount cannot be negative.");
        if (priorRefundedQuantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(priorRefundedQuantity), priorRefundedQuantity, "Prior refunded quantity cannot be negative.");
        if (priorDiscountReversed < 0m)
            throw new ArgumentOutOfRangeException(nameof(priorDiscountReversed), priorDiscountReversed, "Prior discount reversed cannot be negative.");
        if (priorTaxReversed < 0m)
            throw new ArgumentOutOfRangeException(nameof(priorTaxReversed), priorTaxReversed, "Prior tax reversed cannot be negative.");
        if (priorGrossRefunded is < 0m)
            throw new ArgumentOutOfRangeException(nameof(priorGrossRefunded), priorGrossRefunded, "Prior gross refunded cannot be negative.");

        var originalGross = MoneyRoundingPolicy.RoundMoney(originalQuantity * unitPrice);
        if (originalQuantity > 0m)
        {
            if (priorRefundedQuantity > originalQuantity)
            {
                throw new InvalidOperationException(
                    $"Inconsistent refund history: prior refunded quantity ({priorRefundedQuantity}) exceeds original quantity ({originalQuantity}).");
            }

            if (priorDiscountReversed > allocatedDiscount)
            {
                throw new InvalidOperationException(
                    $"Inconsistent refund history: prior discount reversed ({priorDiscountReversed:N2}) exceeds allocated discount ({allocatedDiscount:N2}).");
            }

            if (priorTaxReversed > taxAmount)
            {
                throw new InvalidOperationException(
                    $"Inconsistent refund history: prior tax reversed ({priorTaxReversed:N2}) exceeds original tax ({taxAmount:N2}).");
            }

            if (priorGrossRefunded.HasValue && priorGrossRefunded.Value > originalGross)
            {
                throw new InvalidOperationException(
                    $"Inconsistent refund history: prior gross refunded ({priorGrossRefunded.Value:N2}) exceeds original line gross ({originalGross:N2}).");
            }
        }

        if (requestedQuantity <= 0m || originalQuantity <= 0m)
        {
            return new RefundLineCalculation(0m, 0m, 0m, 0m);
        }

        var remainingQuantity = originalQuantity - priorRefundedQuantity;
        if (requestedQuantity > remainingQuantity)
        {
            throw new InvalidOperationException(
                $"Requested refund quantity ({requestedQuantity}) exceeds remaining eligible quantity ({remainingQuantity}).");
        }

        var effectivePriorGross = priorGrossRefunded ?? MoneyRoundingPolicy.RoundMoney(priorRefundedQuantity * unitPrice);
        var remainingGross = originalGross - effectivePriorGross;
        var remainingDiscount = allocatedDiscount - priorDiscountReversed;
        var remainingTax = taxAmount - priorTaxReversed;

        decimal grossRefund;
        decimal discountReversed;
        decimal taxReversed;

        if (requestedQuantity == remainingQuantity)
        {
            // Final partial or full refund of the remaining line: absorbs exact residual cents across gross, discount, and tax
            grossRefund = remainingGross;
            discountReversed = remainingDiscount;
            taxReversed = remainingTax;
        }
        else
        {
            // Intermediate partial refund: proportional allocation rounded AwayFromZero, capped to remaining reversible balance
            var ratio = requestedQuantity / originalQuantity;
            grossRefund = Math.Min(remainingGross, MoneyRoundingPolicy.RoundMoney(requestedQuantity * unitPrice));
            discountReversed = Math.Min(remainingDiscount, MoneyRoundingPolicy.RoundMoney(allocatedDiscount * ratio));
            taxReversed = Math.Min(remainingTax, MoneyRoundingPolicy.RoundMoney(taxAmount * ratio));
        }

        var netPayableRefund = grossRefund - discountReversed + (taxIsInclusive ? 0m : taxReversed);

        return new RefundLineCalculation(grossRefund, discountReversed, taxReversed, netPayableRefund);
    }
}
