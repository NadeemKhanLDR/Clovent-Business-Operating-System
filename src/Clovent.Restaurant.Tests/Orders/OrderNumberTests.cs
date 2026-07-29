using Clovent.Restaurant.Orders.ValueObjects;
using Xunit;

namespace Clovent.Restaurant.Tests.Orders;

public class OrderNumberTests
{
    [Fact]
    public void Generate_ProducesOrdPrefixedValue()
    {
        var number = OrderNumber.Generate(DateTimeOffset.UtcNow);

        Assert.StartsWith("ORD-", number.Value);
    }

    [Fact]
    public void Generate_DifferentInstants_ProduceDifferentNumbers()
    {
        var first = OrderNumber.Generate(DateTimeOffset.UtcNow);
        var second = OrderNumber.Generate(DateTimeOffset.UtcNow.AddTicks(1));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => OrderNumber.Create(""));
    }

    [Fact]
    public void Create_Valid_RoundTrips()
    {
        var number = OrderNumber.Create("ORD-123");

        Assert.Equal("ORD-123", number.Value);
    }
}
