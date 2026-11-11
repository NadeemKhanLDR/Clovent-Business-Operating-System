using Clovent.Restaurant.DomainServices;
using Xunit;

namespace Clovent.Restaurant.Tests.Refunds;

public class RefundCalculatorTests
{
    [Fact]
    public void CalculateLineRefund_ZeroOrNegativeQuantity_ReturnsZero()
    {
        var zeroCalc = RefundCalculator.CalculateLineRefund(
            originalQuantity: 2m,
            unitPrice: 340m,
            allocatedDiscount: 68m,
            taxAmount: 0m,
            taxIsInclusive: false,
            priorRefundedQuantity: 0m,
            priorDiscountReversed: 0m,
            priorTaxReversed: 0m,
            requestedQuantity: 0m);

        Assert.Equal(0m, zeroCalc.GrossRefund);
        Assert.Equal(0m, zeroCalc.DiscountReversed);
        Assert.Equal(0m, zeroCalc.TaxReversed);
        Assert.Equal(0m, zeroCalc.NetPayableRefund);

        var negCalc = RefundCalculator.CalculateLineRefund(
            originalQuantity: 2m,
            unitPrice: 340m,
            allocatedDiscount: 68m,
            taxAmount: 0m,
            taxIsInclusive: false,
            priorRefundedQuantity: 0m,
            priorDiscountReversed: 0m,
            priorTaxReversed: 0m,
            requestedQuantity: -1m);

        Assert.Equal(0m, negCalc.NetPayableRefund);
    }

    [Fact]
    public void CalculateLineRefund_TaxExemptItemWithAllocatedDiscount_CalculatesNetPayableAsGrossMinusDiscount()
    {
        // Scenario: 1x White Daal Mash from ORD-1 (Ordered 2 @ 340, Total Gross 680, 10% Allocated Discount 68, Tax Exempt 0)
        var calc = RefundCalculator.CalculateLineRefund(
            originalQuantity: 2m,
            unitPrice: 340m,
            allocatedDiscount: 68m,
            taxAmount: 0m,
            taxIsInclusive: false,
            priorRefundedQuantity: 0m,
            priorDiscountReversed: 0m,
            priorTaxReversed: 0m,
            requestedQuantity: 1m);

        Assert.Equal(340m, calc.GrossRefund);
        Assert.Equal(34m, calc.DiscountReversed);
        Assert.Equal(0m, calc.TaxReversed);
        Assert.Equal(306m, calc.NetPayableRefund);
    }

    [Fact]
    public void CalculateLineRefund_SequentialPartialRefunds_AbsorbsResidualRoundingOnFinalPartial()
    {
        // Line with 3 units @ 100 each = 300 gross. Allocated discount = 10 (3.333... per unit).
        // First refund: 1 unit -> ratio 1/3 -> discount 3.33 AwayFromZero. Gross 100, Net 96.67.
        var firstCalc = RefundCalculator.CalculateLineRefund(
            originalQuantity: 3m,
            unitPrice: 100m,
            allocatedDiscount: 10m,
            taxAmount: 0m,
            taxIsInclusive: false,
            priorRefundedQuantity: 0m,
            priorDiscountReversed: 0m,
            priorTaxReversed: 0m,
            requestedQuantity: 1m);

        Assert.Equal(100m, firstCalc.GrossRefund);
        Assert.Equal(3.33m, firstCalc.DiscountReversed);
        Assert.Equal(96.67m, firstCalc.NetPayableRefund);

        // Second refund: 1 unit -> ratio 1/3 -> discount 3.33. Prior refunded: 1, prior discount: 3.33.
        var secondCalc = RefundCalculator.CalculateLineRefund(
            originalQuantity: 3m,
            unitPrice: 100m,
            allocatedDiscount: 10m,
            taxAmount: 0m,
            taxIsInclusive: false,
            priorRefundedQuantity: 1m,
            priorDiscountReversed: 3.33m,
            priorTaxReversed: 0m,
            requestedQuantity: 1m);

        Assert.Equal(100m, secondCalc.GrossRefund);
        Assert.Equal(3.33m, secondCalc.DiscountReversed);
        Assert.Equal(96.67m, secondCalc.NetPayableRefund);

        // Third and final refund: 1 unit (remaining 1). Absorbs residual discount: 10 - (3.33 + 3.33) = 3.34.
        var finalCalc = RefundCalculator.CalculateLineRefund(
            originalQuantity: 3m,
            unitPrice: 100m,
            allocatedDiscount: 10m,
            taxAmount: 0m,
            taxIsInclusive: false,
            priorRefundedQuantity: 2m,
            priorDiscountReversed: 6.66m,
            priorTaxReversed: 0m,
            requestedQuantity: 1m);

        Assert.Equal(100m, finalCalc.GrossRefund);
        Assert.Equal(3.34m, finalCalc.DiscountReversed);
        Assert.Equal(96.66m, finalCalc.NetPayableRefund);

        // Total discount reversed across all 3 partials must equal exactly 10.00
        Assert.Equal(10.00m, firstCalc.DiscountReversed + secondCalc.DiscountReversed + finalCalc.DiscountReversed);
        // Total net refunded across all 3 partials must equal exactly 290.00 (300 - 10)
        Assert.Equal(290.00m, firstCalc.NetPayableRefund + secondCalc.NetPayableRefund + finalCalc.NetPayableRefund);
    }

    [Fact]
    public void CalculateLineRefund_ExclusiveTaxItemWithAllocatedDiscount_IncludesTaxInNetPayable()
    {
        // Scenario: 2x Chicken Karahi from ORD-1 (Ordered 2 @ 2000 = 4000 gross, 10% Allocated Discount 400, Base 3600, PRA 16% Exclusive Tax 576)
        var fullCalc = RefundCalculator.CalculateLineRefund(
            originalQuantity: 2m,
            unitPrice: 2000m,
            allocatedDiscount: 400m,
            taxAmount: 576m,
            taxIsInclusive: false,
            priorRefundedQuantity: 0m,
            priorDiscountReversed: 0m,
            priorTaxReversed: 0m,
            requestedQuantity: 2m);

        Assert.Equal(4000m, fullCalc.GrossRefund);
        Assert.Equal(400m, fullCalc.DiscountReversed);
        Assert.Equal(576m, fullCalc.TaxReversed);
        // Net Payable = 4000 - 400 + 576 = 4,176.00
        Assert.Equal(4176m, fullCalc.NetPayableRefund);

        // Partial: 1 pot
        var partialCalc = RefundCalculator.CalculateLineRefund(
            originalQuantity: 2m,
            unitPrice: 2000m,
            allocatedDiscount: 400m,
            taxAmount: 576m,
            taxIsInclusive: false,
            priorRefundedQuantity: 0m,
            priorDiscountReversed: 0m,
            priorTaxReversed: 0m,
            requestedQuantity: 1m);

        Assert.Equal(2000m, partialCalc.GrossRefund);
        Assert.Equal(200m, partialCalc.DiscountReversed);
        Assert.Equal(288m, partialCalc.TaxReversed);
        // Net Payable = 2000 - 200 + 288 = 2,088.00
        Assert.Equal(2088m, partialCalc.NetPayableRefund);
    }

    [Fact]
    public void CalculateLineRefund_InclusiveTax_DoesNotAddTaxToNetPayable()
    {
        // 1 item @ 116 inclusive of 16% tax (Base 100, Tax 16). No discount.
        var calc = RefundCalculator.CalculateLineRefund(
            originalQuantity: 1m,
            unitPrice: 116m,
            allocatedDiscount: 0m,
            taxAmount: 16m,
            taxIsInclusive: true,
            priorRefundedQuantity: 0m,
            priorDiscountReversed: 0m,
            priorTaxReversed: 0m,
            requestedQuantity: 1m);

        Assert.Equal(116m, calc.GrossRefund);
        Assert.Equal(0m, calc.DiscountReversed);
        Assert.Equal(16m, calc.TaxReversed);
        // Tax is inclusive, so net payable refund is gross (116) - discount (0) = 116
        Assert.Equal(116m, calc.NetPayableRefund);
    }

    [Fact]
    public void CalculateLineRefund_IntermediateFractionalRefund_NeverExceedsRemainingDiscountOrTax()
    {
        // Prior intermediate refunds reversed 9.99 of 10.00 discount and 15.99 of 16.00 tax.
        // An intermediate refund with ratio 0.40 would round to 4.00/6.40 if unclamped, exceeding total allocated.
        var calc = RefundCalculator.CalculateLineRefund(
            originalQuantity: 1m,
            unitPrice: 100m,
            allocatedDiscount: 10m,
            taxAmount: 16m,
            taxIsInclusive: false,
            priorRefundedQuantity: 0.50m,
            priorDiscountReversed: 9.99m,
            priorTaxReversed: 15.99m,
            requestedQuantity: 0.40m);

        Assert.Equal(0.01m, calc.DiscountReversed);
        Assert.Equal(0.01m, calc.TaxReversed);
    }

    [Fact]
    public void CalculateLineRefund_MidpointUnitPriceSplitAcrossTwoHalves_FinalPartialAbsorbsGrossRoundingResidual()
    {
        // 1 unit @ 10.05 -> gross = 10.05.
        // First half 0.5: 5.025 -> AwayFromZero = 5.03.
        // Second half (final remaining 0.5) must absorb residual gross cent (10.05 - 5.03 = 5.02) so total = 10.05, not 10.06.
        var first = RefundCalculator.CalculateLineRefund(
            originalQuantity: 1m,
            unitPrice: 10.05m,
            allocatedDiscount: 0m,
            taxAmount: 0m,
            taxIsInclusive: false,
            priorRefundedQuantity: 0m,
            priorDiscountReversed: 0m,
            priorTaxReversed: 0m,
            requestedQuantity: 0.5m);

        var second = RefundCalculator.CalculateLineRefund(
            originalQuantity: 1m,
            unitPrice: 10.05m,
            allocatedDiscount: 0m,
            taxAmount: 0m,
            taxIsInclusive: false,
            priorRefundedQuantity: 0.5m,
            priorDiscountReversed: 0m,
            priorTaxReversed: 0m,
            requestedQuantity: 0.5m);

        Assert.Equal(5.03m, first.GrossRefund);
        Assert.Equal(5.02m, second.GrossRefund);
        Assert.Equal(10.05m, first.NetPayableRefund + second.NetPayableRefund);
    }

    [Theory]
    [InlineData(2.5, 0, 0)] // priorRefundedQuantity > originalQuantity (2)
    [InlineData(1, 15, 0)]  // priorDiscountReversed > allocatedDiscount (10)
    [InlineData(1, 0, 20)]  // priorTaxReversed > taxAmount (16)
    public void CalculateLineRefund_InconsistentHistoricalState_ThrowsInsteadOfSilentlyClamping(
        decimal priorRefundedQuantity,
        decimal priorDiscountReversed,
        decimal priorTaxReversed)
    {
        Assert.Throws<InvalidOperationException>(() => RefundCalculator.CalculateLineRefund(
            originalQuantity: 2m,
            unitPrice: 100m,
            allocatedDiscount: 10m,
            taxAmount: 16m,
            taxIsInclusive: false,
            priorRefundedQuantity: priorRefundedQuantity,
            priorDiscountReversed: priorDiscountReversed,
            priorTaxReversed: priorTaxReversed,
            requestedQuantity: 0.5m));
    }

    [Fact]
    public void CalculateLineRefund_RequestedQuantityExceedingRemaining_ThrowsInsteadOfSilentlyClamping()
    {
        Assert.Throws<InvalidOperationException>(() => RefundCalculator.CalculateLineRefund(
            originalQuantity: 2m,
            unitPrice: 100m,
            allocatedDiscount: 10m,
            taxAmount: 16m,
            taxIsInclusive: false,
            priorRefundedQuantity: 1.5m,
            priorDiscountReversed: 7.50m,
            priorTaxReversed: 12.00m,
            requestedQuantity: 1m));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CalculateLineRefund_RepeatingFractionalReturnsWithExplicitPriorGross_PreservesExactCumulativeTotals(bool taxIsInclusive)
    {
        // 1 unit @ 10.01 gross, discount 1.00, tax 1.44 -> 0.33 + 0.33 + 0.34
        // Intermediate 0.33 * 10.01 = 3.3033 -> 3.30 (two partials = 6.60).
        // Final 0.34 absorbs exact residual 10.01 - 6.60 = 3.41 (not 3.40 which loses $0.01).
        var p1 = RefundCalculator.CalculateLineRefund(1m, 10.01m, 1.00m, 1.44m, taxIsInclusive, 0m, 0m, 0m, 0.33m, 0m);
        var p2 = RefundCalculator.CalculateLineRefund(1m, 10.01m, 1.00m, 1.44m, taxIsInclusive, 0.33m, p1.DiscountReversed, p1.TaxReversed, 0.33m, p1.GrossRefund);
        var p3 = RefundCalculator.CalculateLineRefund(1m, 10.01m, 1.00m, 1.44m, taxIsInclusive, 0.66m, p1.DiscountReversed + p2.DiscountReversed, p1.TaxReversed + p2.TaxReversed, 0.34m, p1.GrossRefund + p2.GrossRefund);

        Assert.Equal(3.30m, p1.GrossRefund);
        Assert.Equal(3.30m, p2.GrossRefund);
        Assert.Equal(3.41m, p3.GrossRefund);
        Assert.Equal(10.01m, p1.GrossRefund + p2.GrossRefund + p3.GrossRefund);
        Assert.Equal(1.00m, p1.DiscountReversed + p2.DiscountReversed + p3.DiscountReversed);
        Assert.Equal(1.44m, p1.TaxReversed + p2.TaxReversed + p3.TaxReversed);
        var expectedNet = taxIsInclusive ? (10.01m - 1.00m) : (10.01m - 1.00m + 1.44m);
        Assert.Equal(expectedNet, p1.NetPayableRefund + p2.NetPayableRefund + p3.NetPayableRefund);
    }
}
