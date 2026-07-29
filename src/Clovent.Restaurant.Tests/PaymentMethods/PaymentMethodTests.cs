using Clovent.Restaurant;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.Events;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Shared;
using Xunit;

namespace Clovent.Restaurant.Tests.PaymentMethods;

public class PaymentMethodTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesPaymentMethodCreated()
    {
        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));

        Assert.Equal("Cash", method.Name.Value);
        Assert.Equal(RestaurantStatus.Active, method.Status);
        Assert.IsType<PaymentMethodCreated>(Assert.Single(method.DomainEvents));
    }

    [Fact]
    public void Rename_DifferentName_RaisesPaymentMethodRenamed()
    {
        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        method.ClearDomainEvents();

        method.Rename(PaymentMethodName.Create("Credit Card"));

        Assert.IsType<PaymentMethodRenamed>(Assert.Single(method.DomainEvents));
    }

    [Fact]
    public void Deactivate_ThenActivate_RaisesExpectedEvents()
    {
        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        method.ClearDomainEvents();

        method.Deactivate();
        Assert.IsType<PaymentMethodDeactivated>(Assert.Single(method.DomainEvents));

        method.ClearDomainEvents();
        method.Activate();
        Assert.IsType<PaymentMethodActivated>(Assert.Single(method.DomainEvents));
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        method.Deactivate();

        Assert.Throws<RestaurantDomainException>(() => method.Deactivate());
    }
}
