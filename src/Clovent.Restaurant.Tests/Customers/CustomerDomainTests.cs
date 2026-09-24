using System;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.Customers;
using Xunit;

namespace Clovent.Restaurant.Tests.Customers;

public class CustomerDomainTests
{
    [Fact]
    public void Create_WithDefaultTrue_SetsIsDefault()
    {
        var code = EntityCode.Create("C000");
        var customer = Customer.Create(code, "Walk-in Customer", "000-000", "Store", null, 0m, 0m, "System default", isDefault: true);

        Assert.True(customer.IsDefault);
        Assert.True(customer.IsActive);
        Assert.Equal("C000", customer.Code.Value);
    }

    [Fact]
    public void Create_DefaultFalse_IsNotDefault()
    {
        var code = EntityCode.Create("C001");
        var customer = Customer.Create(code, "Standard Customer", "111-222", "Address", null, 0m, 0m, null, isDefault: false);

        Assert.False(customer.IsDefault);
    }

    [Fact]
    public void SetDefault_OnActiveCustomer_Succeeds()
    {
        var customer = Customer.Create(EntityCode.Create("C001"), "Standard Customer", "111-222", "Address", null, 0m, 0m, null);
        Assert.False(customer.IsDefault);

        customer.SetDefault(true);
        Assert.True(customer.IsDefault);

        customer.SetDefault(false);
        Assert.False(customer.IsDefault);
    }

    [Fact]
    public void SetDefault_OnInactiveCustomer_ThrowsDomainException()
    {
        var customer = Customer.Create(EntityCode.Create("C001"), "Standard Customer", "111-222", "Address", null, 0m, 0m, null);
        customer.SetStatus(false);

        Assert.Throws<RestaurantDomainException>(() => customer.SetDefault(true));
    }

    [Fact]
    public void SetStatus_False_AutomaticallyDropsDefault()
    {
        var customer = Customer.Create(EntityCode.Create("C001"), "Standard Customer", "111-222", "Address", null, 0m, 0m, null, isDefault: true);
        Assert.True(customer.IsDefault);

        customer.SetStatus(false);
        Assert.False(customer.IsActive);
        Assert.False(customer.IsDefault);
    }
}
