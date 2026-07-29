using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.ServiceCharges.Commands;
using Clovent.Restaurant.Application.ServiceCharges.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.ServiceCharges;

public class ServiceChargeHandlerTests
{
    [Fact]
    public async Task ApplyServiceChargeToOrderCommandHandler_Valid_AppliesToOrder()
    {
        var orderRepository = new FakeOrderRepository();
        var serviceChargeRepository = new FakeServiceChargeRepository();
        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), TableId.New());
        orderRepository.Add(order);

        var handler = new ApplyServiceChargeToOrderCommandHandler(orderRepository, serviceChargeRepository);
        var result = await handler.Handle(new ApplyServiceChargeToOrderCommand(order.Id.Value, ServiceChargeType.Percentage, 12m, "Large party"), CancellationToken.None);

        Assert.Contains(new ServiceChargeId(result.ServiceChargeId), order.ServiceChargeIds);
    }

    [Fact]
    public async Task RemoveServiceChargeFromOrderCommandHandler_Valid_RemovesFromOrder()
    {
        var orderRepository = new FakeOrderRepository();
        var serviceChargeRepository = new FakeServiceChargeRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var charge = ServiceCharge.Create(order.Id, ServiceChargeType.FixedAmount, 5m, "Fee");
        order.ApplyServiceCharge(charge.Id);
        orderRepository.Add(order);
        serviceChargeRepository.Add(charge);

        var handler = new RemoveServiceChargeFromOrderCommandHandler(orderRepository, serviceChargeRepository);
        await handler.Handle(new RemoveServiceChargeFromOrderCommand(order.Id.Value, charge.Id.Value), CancellationToken.None);

        Assert.DoesNotContain(charge.Id, order.ServiceChargeIds);
    }

    [Fact]
    public async Task ListServiceChargesByOrderQueryHandler_FiltersToOrder()
    {
        var repository = new FakeServiceChargeRepository();
        var orderId = OrderId.New();
        repository.Add(ServiceCharge.Create(orderId, ServiceChargeType.Percentage, 10m, "A"));
        repository.Add(ServiceCharge.Create(OrderId.New(), ServiceChargeType.Percentage, 10m, "B"));

        var result = await new ListServiceChargesByOrderQueryHandler(repository).Handle(new ListServiceChargesByOrderQuery(orderId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
