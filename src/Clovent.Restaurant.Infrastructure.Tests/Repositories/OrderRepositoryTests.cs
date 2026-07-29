using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

public class OrderRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var tableId = TableId.New();
        var warehouseId = WarehouseId.New();
        var order = Order.Create(OrderType.DineIn, warehouseId, tableId);
        order.SetNotes("Extra napkins");
        order.SetCustomerNotes("Birthday");

        await using (var writeContext = CreateContext())
        {
            var repository = new OrderRepository(writeContext);
            await repository.AddAsync(order);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new OrderRepository(readContext).GetByIdAsync(order.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(order.OrderNumber, reloaded!.OrderNumber);
        Assert.Equal(OrderType.DineIn, reloaded.OrderType);
        Assert.Equal(tableId, reloaded.TableId);
        Assert.Equal(warehouseId, reloaded.WarehouseId);
        Assert.Equal("Extra napkins", reloaded.Notes);
        Assert.Equal("Birthday", reloaded.CustomerNotes);
        Assert.Equal(OrderStatus.Open, reloaded.Status);
    }

    [Fact]
    public async Task AddAsync_WithLineDiscountServiceChargePaymentIds_RoundTripsIdLists()
    {
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var lineId = Clovent.Restaurant.OrderLines.OrderLineId.New();
        var discountId = Clovent.Restaurant.Discounts.DiscountId.New();
        var serviceChargeId = Clovent.Restaurant.ServiceCharges.ServiceChargeId.New();
        var paymentId = Clovent.Restaurant.Payments.PaymentId.New();
        order.AddOrderLine(lineId);
        order.ApplyDiscount(discountId);
        order.ApplyServiceCharge(serviceChargeId);
        order.RecordPayment(paymentId);

        await using (var writeContext = CreateContext())
        {
            var repository = new OrderRepository(writeContext);
            await repository.AddAsync(order);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new OrderRepository(readContext).GetByIdAsync(order.Id);

        Assert.NotNull(reloaded);
        Assert.Contains(lineId, reloaded!.OrderLineIds);
        Assert.Contains(discountId, reloaded.DiscountIds);
        Assert.Contains(serviceChargeId, reloaded.ServiceChargeIds);
        Assert.Contains(paymentId, reloaded.PaymentIds);
    }

    [Fact]
    public async Task GetOpenOrHeldByTableIdAsync_FiltersCorrectly()
    {
        var tableId = TableId.New();
        var openOrder = Order.Create(OrderType.DineIn, WarehouseId.New(), tableId);
        var completedOrder = Order.Create(OrderType.DineIn, WarehouseId.New(), TableId.New());
        completedOrder.Complete();

        await using (var writeContext = CreateContext())
        {
            var repository = new OrderRepository(writeContext);
            await repository.AddAsync(openOrder);
            await repository.AddAsync(completedOrder);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new OrderRepository(readContext).GetOpenOrHeldByTableIdAsync(tableId);

        Assert.Single(found);
        Assert.Equal(openOrder.Id, found.First().Id);
    }

    [Fact]
    public async Task GetOpenAsync_And_GetHeldAsync_FilterByStatus()
    {
        var open = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var held = Order.Create(OrderType.TakeAway, WarehouseId.New());
        held.Hold();

        await using (var writeContext = CreateContext())
        {
            var repository = new OrderRepository(writeContext);
            await repository.AddAsync(open);
            await repository.AddAsync(held);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var repo = new OrderRepository(readContext);
        var openResults = await repo.GetOpenAsync();
        var heldResults = await repo.GetHeldAsync();

        Assert.Single(openResults);
        Assert.Single(heldResults);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new OrderRepository(context).GetByIdAsync(OrderId.New());

        Assert.Null(result);
    }
}
