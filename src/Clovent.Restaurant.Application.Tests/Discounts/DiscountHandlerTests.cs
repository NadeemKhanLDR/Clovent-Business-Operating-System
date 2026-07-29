using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Discounts.Commands;
using Clovent.Restaurant.Application.Discounts.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Discounts;

public class DiscountHandlerTests
{
    [Fact]
    public async Task ApplyDiscountToOrderCommandHandler_Valid_AppliesToOrder()
    {
        var orderRepository = new FakeOrderRepository();
        var discountRepository = new FakeDiscountRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = new ApplyDiscountToOrderCommandHandler(orderRepository, discountRepository);
        var result = await handler.Handle(new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.Percentage, 10m, "Loyalty"), CancellationToken.None);

        Assert.Contains(new DiscountId(result.DiscountId), order.DiscountIds);
    }

    [Fact]
    public async Task RemoveDiscountFromOrderCommandHandler_Valid_RemovesFromOrder()
    {
        var orderRepository = new FakeOrderRepository();
        var discountRepository = new FakeDiscountRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var discount = Discount.Create(order.Id, DiscountType.FixedAmount, 5m, "Comp");
        order.ApplyDiscount(discount.Id);
        orderRepository.Add(order);
        discountRepository.Add(discount);

        var handler = new RemoveDiscountFromOrderCommandHandler(orderRepository, discountRepository);
        await handler.Handle(new RemoveDiscountFromOrderCommand(order.Id.Value, discount.Id.Value), CancellationToken.None);

        Assert.DoesNotContain(discount.Id, order.DiscountIds);
    }

    [Fact]
    public async Task ListDiscountsByOrderQueryHandler_FiltersToOrder()
    {
        var repository = new FakeDiscountRepository();
        var orderId = OrderId.New();
        repository.Add(Discount.Create(orderId, DiscountType.Percentage, 10m, "A"));
        repository.Add(Discount.Create(OrderId.New(), DiscountType.Percentage, 10m, "B"));

        var result = await new ListDiscountsByOrderQueryHandler(repository).Handle(new ListDiscountsByOrderQuery(orderId.Value), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetDiscountByIdQueryHandler_NotFound_Throws()
    {
        var handler = new GetDiscountByIdQueryHandler(new FakeDiscountRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetDiscountByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
