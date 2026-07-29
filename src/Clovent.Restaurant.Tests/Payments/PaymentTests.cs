using Clovent.Restaurant;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Payments.Events;
using Xunit;

namespace Clovent.Restaurant.Tests.Payments;

public class PaymentTests
{
    private static Payment CreatePayment(decimal amount = 25.50m) =>
        Payment.Create(OrderId.New(), PaymentMethodId.New(), amount);

    [Fact]
    public void Create_Valid_NotVoidedByDefault_RaisesPaymentCreated()
    {
        var payment = CreatePayment();

        Assert.False(payment.IsVoided);
        Assert.IsType<PaymentCreated>(Assert.Single(payment.DomainEvents));
    }

    [Fact]
    public void Create_NonPositiveAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Payment.Create(OrderId.New(), PaymentMethodId.New(), 0));
    }

    [Fact]
    public void Void_NotVoided_Succeeds()
    {
        var payment = CreatePayment();
        payment.ClearDomainEvents();

        payment.Void();

        Assert.True(payment.IsVoided);
        Assert.IsType<PaymentVoided>(Assert.Single(payment.DomainEvents));
    }

    [Fact]
    public void Void_AlreadyVoided_Throws()
    {
        var payment = CreatePayment();
        payment.Void();

        Assert.Throws<RestaurantDomainException>(() => payment.Void());
    }
}
