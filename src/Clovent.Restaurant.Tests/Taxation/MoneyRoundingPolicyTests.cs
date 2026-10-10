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
    [InlineData(-10.005, -10.01)] // AwayFromZero symmetric
    public void RoundMoney_AppliesAwayFromZero_TwoDecimals(decimal input, decimal expected)
    {
        var actual = MoneyRoundingPolicy.RoundMoney(input);
        Assert.Equal(expected, actual);
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
