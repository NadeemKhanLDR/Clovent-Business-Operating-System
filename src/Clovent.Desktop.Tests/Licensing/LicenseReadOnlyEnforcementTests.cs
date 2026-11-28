using Clovent.Desktop.Licensing;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Refunds.Commands;
using Xunit;

namespace Clovent.Desktop.Tests.Licensing;

public sealed class LicenseReadOnlyEnforcementTests
{
    [Fact]
    public async Task ExpiredLicense_BlocksCommercialMutations_AtApplicationBoundary()
    {
        var behavior = new LicenseReadOnlyEnforcementBehavior<CreateOrderCommand, Clovent.Restaurant.Application.Orders.Dtos.OrderDto>(
            () => LicenseStatus.Expired);

        var called = false;
        await Assert.ThrowsAsync<LicenseReadOnlyException>(() =>
            behavior.Handle(
                new CreateOrderCommand(Clovent.Restaurant.Orders.OrderType.TakeAway, Guid.NewGuid()),
                () =>
                {
                    called = true;
                    return Task.FromResult<Clovent.Restaurant.Application.Orders.Dtos.OrderDto>(null!);
                },
                CancellationToken.None));

        Assert.False(called);
    }

    [Fact]
    public async Task ExpiredLicense_BlocksPaymentsAndRefunds_AtApplicationBoundary()
    {
        var payBehavior = new LicenseReadOnlyEnforcementBehavior<RecordPaymentCommand, Clovent.Restaurant.Application.Payments.Dtos.PaymentDto>(
            () => LicenseStatus.Expired);
        await Assert.ThrowsAsync<LicenseReadOnlyException>(() =>
            payBehavior.Handle(
                new RecordPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 100m),
                () => Task.FromResult<Clovent.Restaurant.Application.Payments.Dtos.PaymentDto>(null!),
                CancellationToken.None));

        var refundBehavior = new LicenseReadOnlyEnforcementBehavior<ProcessRefundCommand, Clovent.Restaurant.Application.Refunds.Dtos.RefundDto>(
            () => LicenseStatus.Expired);
        await Assert.ThrowsAsync<LicenseReadOnlyException>(() =>
            refundBehavior.Handle(
                new ProcessRefundCommand(Guid.NewGuid(), [], "Test", Clovent.Restaurant.Refunds.RefundSettlementMethod.CashPayout, "cashier", Guid.NewGuid(), "Cashier"),
                () => Task.FromResult<Clovent.Restaurant.Application.Refunds.Dtos.RefundDto>(null!),
                CancellationToken.None));
    }

    [Fact]
    public async Task ExpiredLicense_AllowsHistoricalQueries_PerPdr0002()
    {
        var queryBehavior = new LicenseReadOnlyEnforcementBehavior<GetOrderByIdQuery, Clovent.Restaurant.Application.Orders.Dtos.OrderDto>(
            () => LicenseStatus.Expired);

        var called = false;
        await queryBehavior.Handle(
            new GetOrderByIdQuery(Guid.NewGuid()),
            () =>
            {
                called = true;
                return Task.FromResult<Clovent.Restaurant.Application.Orders.Dtos.OrderDto>(null!);
            },
            CancellationToken.None);

        Assert.True(called);
    }

    [Fact]
    public async Task ValidLicense_AllowsCommercialMutations()
    {
        var behavior = new LicenseReadOnlyEnforcementBehavior<CreateOrderCommand, Clovent.Restaurant.Application.Orders.Dtos.OrderDto>(
            () => LicenseStatus.Valid);

        var called = false;
        await behavior.Handle(
            new CreateOrderCommand(Clovent.Restaurant.Orders.OrderType.TakeAway, Guid.NewGuid()),
            () =>
            {
                called = true;
                return Task.FromResult<Clovent.Restaurant.Application.Orders.Dtos.OrderDto>(null!);
            },
            CancellationToken.None);

        Assert.True(called);
    }
}
