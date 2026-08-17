using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Orders;

public class OrderTotalsCalculatorTests
{
    private static OrderLineDto CreateLine(decimal quantity, decimal unitPrice, decimal taxRate = 0, bool taxInclusive = false, bool isVoided = false) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantity, unitPrice, unitPrice, false, null, null, null, taxRate, taxInclusive, null, isVoided, quantity * unitPrice, DateTimeOffset.UtcNow);

    private static DiscountDto CreateDiscount(string type, decimal value) => new(Guid.NewGuid(), Guid.NewGuid(), type, value, "Reason", DateTimeOffset.UtcNow);

    private static ServiceChargeDto CreateServiceCharge(string type, decimal value) => new(Guid.NewGuid(), Guid.NewGuid(), type, value, "Reason", DateTimeOffset.UtcNow);

    private static PaymentDto CreatePayment(decimal amount, bool isVoided = false) => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), amount, isVoided, DateTimeOffset.UtcNow);

    [Fact]
    public void Calculate_NoTaxNoDiscountNoServiceChargeNoPayment_GrandTotalEqualsSubtotal()
    {
        var lines = new[] { CreateLine(2, 10m) };

        var totals = OrderTotalsCalculator.Calculate(lines, [], [], []);

        Assert.Equal(20m, totals.Subtotal);
        Assert.Equal(0m, totals.TaxTotal);
        Assert.Equal(20m, totals.GrandTotal);
        Assert.Equal(20m, totals.Balance);
    }

    [Fact]
    public void Calculate_VoidedLine_ExcludedFromSubtotal()
    {
        var lines = new[] { CreateLine(2, 10m), CreateLine(1, 100m, isVoided: true) };

        var totals = OrderTotalsCalculator.Calculate(lines, [], [], []);

        Assert.Equal(20m, totals.Subtotal);
    }

    [Fact]
    public void Calculate_ExclusiveTax_AddedOnTopOfSubtotal()
    {
        var lines = new[] { CreateLine(1, 100m, taxRate: 15m, taxInclusive: false) };

        var totals = OrderTotalsCalculator.Calculate(lines, [], [], []);

        Assert.Equal(100m, totals.Subtotal);
        Assert.Equal(15m, totals.TaxTotal);
        Assert.Equal(115m, totals.GrandTotal);
    }

    [Fact]
    public void Calculate_InclusiveTax_AlreadyPartOfSubtotal_NotAddedAgain()
    {
        var lines = new[] { CreateLine(1, 115m, taxRate: 15m, taxInclusive: true) };

        var totals = OrderTotalsCalculator.Calculate(lines, [], [], []);

        Assert.Equal(115m, totals.Subtotal);
        Assert.True(totals.TaxTotal > 0);
        Assert.Equal(115m, totals.GrandTotal);
    }

    [Fact]
    public void Calculate_PercentageDiscount_ReducesGrandTotal()
    {
        var lines = new[] { CreateLine(1, 100m) };
        var discounts = new[] { CreateDiscount("Percentage", 10m) };

        var totals = OrderTotalsCalculator.Calculate(lines, discounts, [], []);

        Assert.Equal(10m, totals.DiscountTotal);
        Assert.Equal(90m, totals.GrandTotal);
    }

    [Fact]
    public void Calculate_FixedAmountDiscount_ReducesGrandTotalByFlatValue()
    {
        var lines = new[] { CreateLine(1, 100m) };
        var discounts = new[] { CreateDiscount("FixedAmount", 15m) };

        var totals = OrderTotalsCalculator.Calculate(lines, discounts, [], []);

        Assert.Equal(15m, totals.DiscountTotal);
        Assert.Equal(85m, totals.GrandTotal);
    }

    [Fact]
    public void Calculate_PercentageServiceCharge_IncreasesGrandTotal()
    {
        var lines = new[] { CreateLine(1, 100m) };
        var serviceCharges = new[] { CreateServiceCharge("Percentage", 12m) };

        var totals = OrderTotalsCalculator.Calculate(lines, [], serviceCharges, []);

        Assert.Equal(12m, totals.ServiceChargeTotal);
        Assert.Equal(112m, totals.GrandTotal);
    }

    [Fact]
    public void Calculate_Payments_ReduceBalance()
    {
        var lines = new[] { CreateLine(1, 100m) };
        var payments = new[] { CreatePayment(60m) };

        var totals = OrderTotalsCalculator.Calculate(lines, [], [], payments);

        Assert.Equal(60m, totals.PaidTotal);
        Assert.Equal(40m, totals.Balance);
    }

    [Fact]
    public void Calculate_VoidedPayment_ExcludedFromPaidTotal()
    {
        var lines = new[] { CreateLine(1, 100m) };
        var payments = new[] { CreatePayment(60m), CreatePayment(40m, isVoided: true) };

        var totals = OrderTotalsCalculator.Calculate(lines, [], [], payments);

        Assert.Equal(60m, totals.PaidTotal);
        Assert.Equal(40m, totals.Balance);
    }

    [Fact]
    public void Calculate_FullyPaid_BalanceIsZero()
    {
        var lines = new[] { CreateLine(1, 100m) };
        var payments = new[] { CreatePayment(100m) };

        var totals = OrderTotalsCalculator.Calculate(lines, [], [], payments);

        Assert.Equal(0m, totals.Balance);
    }

    [Fact]
    public void Calculate_DiscountAndServiceChargeAndTax_AllCompose()
    {
        var lines = new[] { CreateLine(1, 100m, taxRate: 10m, taxInclusive: false) };
        var discounts = new[] { CreateDiscount("Percentage", 10m) };
        var serviceCharges = new[] { CreateServiceCharge("FixedAmount", 5m) };

        var totals = OrderTotalsCalculator.Calculate(lines, discounts, serviceCharges, []);

        // Subtotal 100, discount 10% of 100 = 10, service charge flat 5, exclusive tax 10% of 100 = 10.
        // GrandTotal = 100 - 10 + 5 + 10 = 105.
        Assert.Equal(105m, totals.GrandTotal);
    }
}
