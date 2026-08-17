using Clovent.Restaurant;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Tests.Orders;

public class OrderNumberSequenceTests
{
    [Fact]
    public void CreateDefault_UsesOrdPrefixStartingAtOne()
    {
        var sequence = OrderNumberSequence.CreateDefault();

        Assert.Equal("ORD-", sequence.Prefix);
        Assert.Equal(1, sequence.NextNumber);
    }

    [Fact]
    public void Next_ReturnsCurrentNumberThenAdvances()
    {
        var sequence = OrderNumberSequence.CreateDefault();

        var first = sequence.Next();
        var second = sequence.Next();

        Assert.Equal("ORD-1", first.Value);
        Assert.Equal("ORD-2", second.Value);
    }

    [Fact]
    public void Configure_RebaselinesPrefixAndNextNumber()
    {
        var sequence = OrderNumberSequence.CreateDefault();

        sequence.Configure("INV-", 3453);

        Assert.Equal("INV-", sequence.Prefix);
        Assert.Equal(3453, sequence.NextNumber);
        Assert.Equal("INV-3453", sequence.Next().Value);
        Assert.Equal(3454, sequence.NextNumber);
    }

    [Fact]
    public void Configure_EmptyPrefix_Throws()
    {
        var sequence = OrderNumberSequence.CreateDefault();

        Assert.Throws<RestaurantDomainException>(() => sequence.Configure("   ", 1));
    }

    [Fact]
    public void Configure_StartingNumberBelowOne_Throws()
    {
        var sequence = OrderNumberSequence.CreateDefault();

        Assert.Throws<RestaurantDomainException>(() => sequence.Configure("ORD-", 0));
    }

    [Fact]
    public void Next_WithVeryShortPrefix_StillProducesAValidOrderNumber()
    {
        var sequence = OrderNumberSequence.CreateDefault();
        sequence.Configure("O", 1);

        var issued = sequence.Next();

        Assert.Equal("O01", issued.Value);
    }
}
