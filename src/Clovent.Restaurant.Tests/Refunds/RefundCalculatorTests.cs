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
}
