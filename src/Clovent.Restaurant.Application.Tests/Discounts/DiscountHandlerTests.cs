using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Discounts.Commands;
using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.Discounts.Queries;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Discounts;

public class DiscountHandlerTests
{
    /// <summary>Builds the handler and gives <paramref name="order"/> one line worth <paramref name="subtotal"/>, so the order-relative discount ceiling has a real bill to measure against.</summary>
    private static ApplyDiscountToOrderCommandHandler CreateApplyHandler(
        FakeOrderRepository orderRepository,
        FakeDiscountRepository discountRepository,
        Order order,
        decimal subtotal)
    {
        var orderLineRepository = new FakeOrderLineRepository();
        if (subtotal > 0)
        {
            var line = OrderLine.Create(order.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, subtotal, 0, false);
            order.AddOrderLine(line.Id);
            orderLineRepository.Add(line);
        }

        return new ApplyDiscountToOrderCommandHandler(orderRepository, discountRepository, orderLineRepository);
    }

    [Fact]
    public async Task ApplyDiscountToOrderCommandHandler_Valid_AppliesToOrder()
    {
        var orderRepository = new FakeOrderRepository();
        var discountRepository = new FakeDiscountRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateApplyHandler(orderRepository, discountRepository, order, subtotal: 100m);
        var result = await handler.Handle(new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.Percentage, 10m, "Loyalty"), CancellationToken.None);

        Assert.Contains(new DiscountId(result.DiscountId), order.DiscountIds);
    }

    /// <summary>M-5 scenario 1: a fixed discount comfortably inside the bill is applied.</summary>
    [Fact]
    public async Task ApplyDiscountToOrderCommandHandler_ValidFixedDiscount_Applies()
    {
        var orderRepository = new FakeOrderRepository();
        var discountRepository = new FakeDiscountRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateApplyHandler(orderRepository, discountRepository, order, subtotal: 100m);
        var result = await handler.Handle(new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.FixedAmount, 30m, "Manager comp"), CancellationToken.None);

        Assert.Equal(30m, result.Value);
        Assert.Contains(new DiscountId(result.DiscountId), order.DiscountIds);
        Assert.Single(await discountRepository.GetByOrderIdAsync(order.Id, CancellationToken.None));
    }

    /// <summary>M-5 scenario 2: a fixed discount exactly equal to the subtotal is legitimate - it comps the bill to zero, not below.</summary>
    [Fact]
    public async Task ApplyDiscountToOrderCommandHandler_FixedDiscountEqualToSubtotal_Applies()
    {
        var orderRepository = new FakeOrderRepository();
        var discountRepository = new FakeDiscountRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateApplyHandler(orderRepository, discountRepository, order, subtotal: 100m);
        var result = await handler.Handle(new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.FixedAmount, 100m, "Full comp"), CancellationToken.None);

        Assert.Equal(100m, result.Value);
        Assert.Contains(new DiscountId(result.DiscountId), order.DiscountIds);
    }

    /// <summary>M-5 scenario 3: a fixed discount larger than the bill is rejected, and nothing is persisted.</summary>
    [Fact]
    public async Task ApplyDiscountToOrderCommandHandler_FixedDiscountGreaterThanSubtotal_Rejected()
    {
        var orderRepository = new FakeOrderRepository();
        var discountRepository = new FakeDiscountRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateApplyHandler(orderRepository, discountRepository, order, subtotal: 50m);

        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.FixedAmount, 500m, "Fat finger"), CancellationToken.None));

        Assert.Contains("more than the order's subtotal", ex.Message);
        Assert.Empty(order.DiscountIds);
        Assert.Empty(await discountRepository.GetByOrderIdAsync(order.Id, CancellationToken.None));
    }

    /// <summary>M-5: the ceiling is cumulative - two discounts that each fit must still be refused when together they exceed the bill.</summary>
    [Fact]
    public async Task ApplyDiscountToOrderCommandHandler_SecondDiscountBreachingSubtotal_Rejected()
    {
        var orderRepository = new FakeOrderRepository();
        var discountRepository = new FakeDiscountRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateApplyHandler(orderRepository, discountRepository, order, subtotal: 50m);

        await handler.Handle(new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.FixedAmount, 30m, "First"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.FixedAmount, 30m, "Second"), CancellationToken.None));

        Assert.Contains("more than the order's subtotal", ex.Message);
        Assert.Single(await discountRepository.GetByOrderIdAsync(order.Id, CancellationToken.None));
    }

    /// <summary>M-5 scenario 4: percentage discounts are untouched, including a full 100% comp.</summary>
    [Fact]
    public async Task ApplyDiscountToOrderCommandHandler_PercentageDiscounts_Unchanged()
    {
        var orderRepository = new FakeOrderRepository();
        var discountRepository = new FakeDiscountRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateApplyHandler(orderRepository, discountRepository, order, subtotal: 200m);

        var result = await handler.Handle(new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.Percentage, 100m, "Full comp"), CancellationToken.None);

        Assert.Equal("Percentage", result.DiscountType);
        Assert.Equal(100m, result.Value);
        Assert.Contains(new DiscountId(result.DiscountId), order.DiscountIds);
    }

    /// <summary>M-5: a percentage above 100 is still the domain's own call, unchanged by the new order-relative ceiling.</summary>
    [Fact]
    public async Task ApplyDiscountToOrderCommandHandler_PercentageAbove100_StillRejectedByDomain()
    {
        var orderRepository = new FakeOrderRepository();
        var discountRepository = new FakeDiscountRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        var handler = CreateApplyHandler(orderRepository, discountRepository, order, subtotal: 200m);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.Handle(new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.Percentage, 150m, "Too much"), CancellationToken.None));

        Assert.Empty(order.DiscountIds);
    }

    /// <summary>
    /// M-5 scenario 5: the ceiling is measured against the subtotal alone, so
    /// tax and service charges keep computing exactly as before - an accepted
    /// discount still leaves a non-negative grand total once they are applied.
    /// </summary>
    [Fact]
    public void OrderTotalsCalculator_WithMaximumDiscount_TaxAndServiceChargeStillCorrect()
    {
        var orderId = OrderId.New();
        var line = OrderLine.Create(orderId, Clovent.Catalog.Variants.ProductVariantId.New(), 2, 50m, 10m, false);
        var discount = Discount.Create(orderId, DiscountType.FixedAmount, 100m, "Full comp");
        var serviceCharge = ServiceCharge.Create(orderId, ServiceChargeType.Percentage, 10m, "Service");

        var totals = OrderTotalsCalculator.Calculate(
            [OrderLineDto.FromDomain(line)],
            [DiscountDto.FromDomain(discount)],
            [ServiceChargeDto.FromDomain(serviceCharge)],
            []);

        Assert.Equal(100m, totals.Subtotal);
        Assert.Equal(100m, totals.DiscountTotal);
        Assert.Equal(10m, totals.TaxTotal);           // 10% exclusive on 100
        Assert.Equal(10m, totals.ServiceChargeTotal); // 10% of 100
        Assert.Equal(20m, totals.GrandTotal);         // 100 - 100 + 10 + 10
        Assert.True(totals.GrandTotal >= 0m);
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
