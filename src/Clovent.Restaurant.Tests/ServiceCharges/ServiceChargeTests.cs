using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.ServiceCharges.Events;
using Xunit;

namespace Clovent.Restaurant.Tests.ServiceCharges;

public class ServiceChargeTests
{
    [Fact]
    public void Create_Percentage_Valid_RaisesServiceChargeCreated()
    {
        var charge = ServiceCharge.Create(OrderId.New(), ServiceChargeType.Percentage, 12m, "Large party gratuity");

        Assert.Equal(ServiceChargeType.Percentage, charge.ServiceChargeType);
        Assert.Equal(12m, charge.Value);
        Assert.IsType<ServiceChargeCreated>(Assert.Single(charge.DomainEvents));
    }

    [Fact]
    public void Create_PercentageOver100_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceCharge.Create(OrderId.New(), ServiceChargeType.Percentage, 101m, "Invalid"));
    }

    [Fact]
    public void Create_NegativeValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceCharge.Create(OrderId.New(), ServiceChargeType.FixedAmount, -1m, "Invalid"));
    }

    [Fact]
    public void Create_EmptyReason_Throws()
    {
        Assert.Throws<ArgumentException>(() => ServiceCharge.Create(OrderId.New(), ServiceChargeType.FixedAmount, 5m, ""));
    }
}
