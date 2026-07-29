using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Orders;

public class GetOrderSummaryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ComputesTotalsFromOrderLines()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var discountRepository = new FakeDiscountRepository();
        var serviceChargeRepository = new FakeServiceChargeRepository();
        var paymentRepository = new FakePaymentRepository();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 2, 10m, 0, false);
        order.AddOrderLine(line.Id);
        orderRepository.Add(order);
        orderLineRepository.Add(line);

        var handler = new GetOrderSummaryQueryHandler(orderRepository, orderLineRepository, discountRepository, serviceChargeRepository, paymentRepository);

        var result = await handler.Handle(new GetOrderSummaryQuery(order.Id.Value), CancellationToken.None);

        Assert.Equal(20m, result.Subtotal);
        Assert.Equal(20m, result.GrandTotal);
        Assert.Equal(20m, result.Balance);
    }

    [Fact]
    public async Task Handle_OrderNotFound_Throws()
    {
        var handler = new GetOrderSummaryQueryHandler(
            new FakeOrderRepository(), new FakeOrderLineRepository(), new FakeDiscountRepository(), new FakeServiceChargeRepository(), new FakePaymentRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetOrderSummaryQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
