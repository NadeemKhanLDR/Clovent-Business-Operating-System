using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Discounts.Events;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Tests.Discounts;

public class DiscountTests
{
    [Fact]
    public void Create_Percentage_Valid_RaisesDiscountCreated()
    {
        var discount = Discount.Create(OrderId.New(), DiscountType.Percentage, 10m, "Loyalty discount");

        Assert.Equal(DiscountType.Percentage, discount.DiscountType);
        Assert.Equal(10m, discount.Value);
        Assert.Equal("Loyalty discount", discount.Reason);
        Assert.IsType<DiscountCreated>(Assert.Single(discount.DomainEvents));
    }

    [Fact]
    public void Create_FixedAmount_Valid_Succeeds()
    {
        var discount = Discount.Create(OrderId.New(), DiscountType.FixedAmount, 5m, "Manager comp");

        Assert.Equal(DiscountType.FixedAmount, discount.DiscountType);
    }

    [Fact]
    public void Create_PercentageOver100_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Discount.Create(OrderId.New(), DiscountType.Percentage, 150m, "Invalid"));
    }

    [Fact]
    public void Create_NegativeValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Discount.Create(OrderId.New(), DiscountType.FixedAmount, -1m, "Invalid"));
    }

    [Fact]
    public void Create_EmptyReason_Throws()
    {
        Assert.Throws<ArgumentException>(() => Discount.Create(OrderId.New(), DiscountType.Percentage, 10m, " "));
    }
}
