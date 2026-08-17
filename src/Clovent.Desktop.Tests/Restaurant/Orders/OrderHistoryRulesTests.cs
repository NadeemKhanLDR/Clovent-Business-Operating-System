using Clovent.Desktop.Restaurant.Orders;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Covers which orders the Order History screen lists. The screen exists so a
/// payment recorded in error on an already-closed bill can still reach Payment
/// History / Void Payment; listing the wrong statuses would either hide the
/// orders that need correcting or open a second, read-only path to a live one.
/// </summary>
public class OrderHistoryRulesTests
{
    [Fact]
    public void CompletedOrderIsListed()
    {
        Assert.True(OrderHistoryRules.IsClosed("Completed"));
    }

    [Fact]
    public void CancelledOrderIsListed()
    {
        Assert.True(OrderHistoryRules.IsClosed("Cancelled"));
    }

    [Fact]
    public void VoidedOrderIsListed()
    {
        Assert.True(OrderHistoryRules.IsClosed("Voided"));
    }

    /// <summary>Open orders belong to Running Orders and stay editable in the POS - this screen must not offer a second route to them.</summary>
    [Fact]
    public void OpenOrderIsNotListed()
    {
        Assert.False(OrderHistoryRules.IsClosed("Open"));
    }

    /// <summary>Held orders belong to Held Orders for the same reason.</summary>
    [Fact]
    public void HeldOrderIsNotListed()
    {
        Assert.False(OrderHistoryRules.IsClosed("Held"));
    }

    [Fact]
    public void UnknownOrMissingStatusIsNotListed()
    {
        Assert.False(OrderHistoryRules.IsClosed(null));
        Assert.False(OrderHistoryRules.IsClosed("Something else"));
    }

    [Fact]
    public void ClosedStatusesAreExactlyCompletedCancelledAndVoided()
    {
        Assert.Equal(["Completed", "Cancelled", "Voided"], OrderHistoryRules.ClosedStatuses);
    }

    /// <summary>Order number and table code are what a cashier has to hand when chasing a bill.</summary>
    [Fact]
    public void SearchTextMatchesOrderNumberTableAndStatus()
    {
        var haystack = OrderHistoryRules.SearchText("ORD-35", "T-02", "Completed", 2);

        Assert.Contains("ORD-35", haystack);
        Assert.Contains("T-02", haystack);
        Assert.Contains("Completed", haystack);
        Assert.Contains("2", haystack);
    }

    [Fact]
    public void SearchTextHandlesAnUnassignedDailySalesNumber()
    {
        var haystack = OrderHistoryRules.SearchText("ORD-4", "-", "Cancelled", null);

        Assert.Contains("ORD-4", haystack);
        Assert.Contains("Cancelled", haystack);
    }
}
