using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Shifts.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Shifts;

public class GetLastCompletedOrderForShiftQueryTests
{
    private readonly FakePaymentRepository _paymentRepo = new();
    private readonly FakeOrderRepository _orderRepo = new();

    private readonly Guid _shiftId1 = Guid.NewGuid();
    private readonly Guid _shiftId2 = Guid.NewGuid();

    [Fact]
    public async Task Handle_WhenNoPaymentsForShift_ReturnsNull()
    {
        var handler = new GetLastCompletedOrderForShiftQueryHandler(_paymentRepo, _orderRepo);

        var result = await handler.Handle(new GetLastCompletedOrderForShiftQuery(_shiftId1), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenOrderNotCompleted_ReturnsNull()
    {
        var order = Order.Create(
            OrderType.TakeAway,
            new WarehouseId(Guid.NewGuid()),
            null,
            OrderNumber.Create("ORD-101"));
        await _orderRepo.AddAsync(order);

        var payment = Payment.Create(
            order.Id,
            new PaymentMethodId(Guid.NewGuid()),
            25m,
            new ShiftId(_shiftId1));
        await _paymentRepo.AddAsync(payment);

        var handler = new GetLastCompletedOrderForShiftQueryHandler(_paymentRepo, _orderRepo);

        var result = await handler.Handle(new GetLastCompletedOrderForShiftQuery(_shiftId1), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenCompletedOrderExists_ReturnsLatestCompletedOrderForShift()
    {
        // First order
        var order1 = Order.Create(
            OrderType.TakeAway,
            new WarehouseId(Guid.NewGuid()),
            null,
            OrderNumber.Create("ORD-101"));
        order1.Complete();
        await _orderRepo.AddAsync(order1);

        var payment1 = Payment.Create(
            order1.Id,
            new PaymentMethodId(Guid.NewGuid()),
            25m,
            new ShiftId(_shiftId1));
        await _paymentRepo.AddAsync(payment1);

        // Second order (more recent)
        var order2 = Order.Create(
            OrderType.TakeAway,
            new WarehouseId(Guid.NewGuid()),
            null,
            OrderNumber.Create("ORD-102"));
        order2.Complete();
        await _orderRepo.AddAsync(order2);

        var payment2 = Payment.Create(
            order2.Id,
            new PaymentMethodId(Guid.NewGuid()),
            50m,
            new ShiftId(_shiftId1));
        await _paymentRepo.AddAsync(payment2);

        // Other shift order
        var orderOther = Order.Create(
            OrderType.TakeAway,
            new WarehouseId(Guid.NewGuid()),
            null,
            OrderNumber.Create("ORD-999"));
        orderOther.Complete();
        await _orderRepo.AddAsync(orderOther);

        var paymentOther = Payment.Create(
            orderOther.Id,
            new PaymentMethodId(Guid.NewGuid()),
            100m,
            new ShiftId(_shiftId2));
        await _paymentRepo.AddAsync(paymentOther);

        var handler = new GetLastCompletedOrderForShiftQueryHandler(_paymentRepo, _orderRepo);

        var result = await handler.Handle(new GetLastCompletedOrderForShiftQuery(_shiftId1), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("ORD-102", result.OrderNumber);
    }
}
