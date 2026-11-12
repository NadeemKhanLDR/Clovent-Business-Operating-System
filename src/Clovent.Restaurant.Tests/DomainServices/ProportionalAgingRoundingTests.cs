using Clovent.Restaurant.DomainServices;
using Xunit;

namespace Clovent.Restaurant.Tests.DomainServices;

public sealed class ProportionalAgingRoundingTests
{
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
