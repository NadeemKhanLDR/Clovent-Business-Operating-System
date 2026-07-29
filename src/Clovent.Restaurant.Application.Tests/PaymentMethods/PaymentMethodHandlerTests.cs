using Clovent.Restaurant.Application.PaymentMethods.Commands;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.PaymentMethods;

public class PaymentMethodHandlerTests
{
    [Fact]
    public async Task CreatePaymentMethodCommandHandler_Valid_Creates()
    {
        var repository = new FakePaymentMethodRepository();

        var result = await new CreatePaymentMethodCommandHandler(repository).Handle(new CreatePaymentMethodCommand("Cash"), CancellationToken.None);

        Assert.Equal("Cash", result.Name);
    }

    [Fact]
    public async Task RenamePaymentMethodCommandHandler_Existing_Renames()
    {
        var repository = new FakePaymentMethodRepository();
        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        repository.Add(method);

        var result = await new RenamePaymentMethodCommandHandler(repository).Handle(new RenamePaymentMethodCommand(method.Id.Value, "Credit Card"), CancellationToken.None);

        Assert.Equal("Credit Card", result.Name);
    }

    [Fact]
    public async Task ActivateThenDeactivate_RoundTrips()
    {
        var repository = new FakePaymentMethodRepository();
        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        method.Deactivate();
        repository.Add(method);

        var activated = await new ActivatePaymentMethodCommandHandler(repository).Handle(new ActivatePaymentMethodCommand(method.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivatePaymentMethodCommandHandler(repository).Handle(new DeactivatePaymentMethodCommand(method.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task ListPaymentMethodsQueryHandler_ReturnsEvery()
    {
        var repository = new FakePaymentMethodRepository();
        repository.Add(PaymentMethod.Create(PaymentMethodName.Create("Cash")));
        repository.Add(PaymentMethod.Create(PaymentMethodName.Create("Credit Card")));

        var result = await new ListPaymentMethodsQueryHandler(repository).Handle(new ListPaymentMethodsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
