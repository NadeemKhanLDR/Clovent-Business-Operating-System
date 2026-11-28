using Clovent.Restaurant.DomainServices;
using Xunit;

namespace Clovent.Restaurant.Tests.Taxation;

public class MoneyRoundingPolicyTests
{
    [Theory]
    [InlineData(10.004, 10.00)]
    [InlineData(10.005, 10.01)]   // AwayFromZero: 0.005 rounds up to 0.01
    [InlineData(10.006, 10.01)]
    [InlineData(10.015, 10.02)]   // AwayFromZero: 0.015 rounds up to 0.02 (not bank's rounding to even 10.01)
    [InlineData(10.025, 10.03)]   // AwayFromZero: 0.025 rounds up to 0.03
    [InlineData(99.995, 100.00)]  // Boundary case
    [InlineData(0.000, 0.00)]     // Zero value
    [InlineData(-10.004, -10.00)]
    [InlineData(-10.005, -10.01)] // AwayFromZero symmetric
    [InlineData(-99.995, -100.00)]
    [InlineData(1234567.895, 1234567.90)] // Large amount
    public void RoundMoney_AppliesAwayFromZero_TwoDecimals(decimal input, decimal expected)
    {
        var actual = MoneyRoundingPolicy.RoundMoney(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(10.005, 0, 10)]      // 0 decimals (e.g. JPY / KRW)
    [InlineData(10.505, 0, 11)]
    [InlineData(-10.505, 0, -11)]
    [InlineData(10.004, 2, 10.00)]   // 2 decimals (e.g. PKR / USD / EUR)
    [InlineData(10.005, 2, 10.01)]
    [InlineData(10.006, 2, 10.01)]
    [InlineData(99.995, 2, 100.00)]
    [InlineData(10.1234, 3, 10.123)] // 3 decimals (e.g. BHD / KWD / OMR)
    [InlineData(10.1235, 3, 10.124)]
    [InlineData(10.1236, 3, 10.124)]
    public void RoundMoney_AppliesAwayFromZero_VariableCurrencyDecimals(decimal input, int decimals, decimal expected)
    {
        var actual = MoneyRoundingPolicy.RoundMoney(input, decimals);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RoundTax_RoundDiscount_RoundServiceCharge_ApplyAwayFromZero()
    {
        Assert.Equal(10.01m, MoneyRoundingPolicy.RoundTax(10.005m));
        Assert.Equal(5.56m, MoneyRoundingPolicy.RoundDiscount(5.555m));
        Assert.Equal(2.50m, MoneyRoundingPolicy.RoundServiceCharge(2.495m));
        Assert.Equal(15.25m, MoneyRoundingPolicy.RoundChange(15.245m));
        Assert.Equal(50.00m, MoneyRoundingPolicy.RoundTender(49.995m));
        Assert.Equal(0.00m, MoneyRoundingPolicy.RoundBalance(0.000m));
    }

    [Theory]
    [InlineData(10.12344, 10.1234)]
    [InlineData(10.12345, 10.1235)]
    [InlineData(10.12346, 10.1235)]
    public void RoundUnitPrice_AppliesAwayFromZero_FourDecimals(decimal input, decimal expected)
    {
        var actual = MoneyRoundingPolicy.RoundUnitPrice(input);
        Assert.Equal(expected, actual);
    }
}
