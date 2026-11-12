using Clovent.Restaurant.DomainServices;
using Xunit;

namespace Clovent.Restaurant.Tests.DomainServices;

public sealed class ProportionalAgingRoundingTests
{
    [Theory]
    [InlineData("0", "0", "0", "0", "0")]
    [InlineData("0.01", "0.005", "0.005", "0", "0.01")]
    [InlineData("0.03", "0.015", "0.015", "0.01", "0.02")]
    [InlineData("9999999999999999.99", "4999999999999999.995", "4999999999999999.995", "4999999999999999.99", "5000000000000000.00")]
    public void BoundaryAmounts_ReconcileExactly(string total, string younger, string older, string expectedYounger, string expectedOlder)
    {
        decimal Parse(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var result = ProportionalAgingRounding.Reconcile(Parse(total), Parse(younger), Parse(older));
        Assert.Equal(Parse(expectedYounger), result[0]);
        Assert.Equal(Parse(expectedOlder), result[1]);
        Assert.Equal(Parse(total), result.Sum());
    }

    [Fact]
    public void CreditsAndInvalidShares_AreNotTurnedIntoReceivableBuckets()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProportionalAgingRounding.Reconcile(-.01m, -.01m));
        Assert.Throws<InvalidOperationException>(() => ProportionalAgingRounding.Reconcile(.01m, .02m));
    }
    [Fact]
    public void EqualRemainders_GivePennyToOlderBucket()
    {
        Assert.Equal([0m, 0m, .01m, 0m, 0m, 0m],
            ProportionalAgingRounding.Reconcile(.01m, 0m, .005m, .005m, 0m, 0m, 0m));
    }

    [Fact]
    public void LargestRemainderPrecedesAge()
    {
        Assert.Equal([.01m, 0m], ProportionalAgingRounding.Reconcile(.01m, .006m, .004m));
    }

    [Fact]
    public void ExactAmountsAndZeroStayUnchanged()
    {
        Assert.Equal([12.34m, 56.78m], ProportionalAgingRounding.Reconcile(69.12m, 12.34m, 56.78m));
        Assert.Equal([0m, 0m], ProportionalAgingRounding.Reconcile(0m, 0m, 0m));
    }
}
