using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Payments.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Payments;

public class PaymentHandlerTests
{
    [Fact]
    public async Task RecordPaymentCommandHandler_Valid_RecordsAgainstOrder()
    {
        var orderRepository = new FakeOrderRepository();
        var paymentRepository = new FakePaymentRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = new RecordPaymentCommandHandler(orderRepository, paymentRepository);
        var result = await handler.Handle(new RecordPaymentCommand(order.Id.Value, PaymentMethodId.New().Value, 25m), CancellationToken.None);

        Assert.Contains(new PaymentId(result.PaymentId), order.PaymentIds);
        Assert.Equal(25m, result.Amount);
    }

    [Fact]
    public async Task VoidPaymentCommandHandler_Valid_Voids()
    {
        var repository = new FakePaymentRepository();
        var payment = Payment.Create(OrderId.New(), PaymentMethodId.New(), 25m);
        repository.Add(payment);

        var result = await new VoidPaymentCommandHandler(repository).Handle(new VoidPaymentCommand(payment.Id.Value), CancellationToken.None);

        Assert.True(result.IsVoided);
    }

    [Fact]
    public async Task ListPaymentsByOrderQueryHandler_FiltersToOrder()
    {
        var repository = new FakePaymentRepository();
        var orderId = OrderId.New();
        repository.Add(Payment.Create(orderId, PaymentMethodId.New(), 10m));
        repository.Add(Payment.Create(OrderId.New(), PaymentMethodId.New(), 20m));

        var result = await new ListPaymentsByOrderQueryHandler(repository).Handle(new ListPaymentsByOrderQuery(orderId.Value), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetPaymentByIdQueryHandler_NotFound_Throws()
    {
        var handler = new GetPaymentByIdQueryHandler(new FakePaymentRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetPaymentByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
