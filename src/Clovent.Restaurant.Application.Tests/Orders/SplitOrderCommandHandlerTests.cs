using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Orders;

public class SplitOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_MovesSelectedLinesToNewOrderAtTargetTable()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var tableRepository = new FakeTableRepository();

        var sourceTable = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 6);
        sourceTable.Occupy();
        var targetTable = Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4);
        tableRepository.Add(sourceTable);
        tableRepository.Add(targetTable);

        var warehouseId = WarehouseId.New();
        var sourceOrder = Order.Create(OrderType.DineIn, warehouseId, sourceTable.Id);
        var lineToMove = OrderLine.Create(sourceOrder.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 10m, 0, false);
        var lineToKeep = OrderLine.Create(sourceOrder.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 1, 8m, 0, false);
        sourceOrder.AddOrderLine(lineToMove.Id);
        sourceOrder.AddOrderLine(lineToKeep.Id);
        orderRepository.Add(sourceOrder);
        orderLineRepository.Add(lineToMove);
        orderLineRepository.Add(lineToKeep);

        var handler = new SplitOrderCommandHandler(orderRepository, orderLineRepository, tableRepository);

        var result = await handler.Handle(new SplitOrderCommand(sourceOrder.Id.Value, [lineToMove.Id.Value], targetTable.Id.Value), CancellationToken.None);

        Assert.Contains(lineToMove.Id.Value, result.OrderLineIds);
        Assert.DoesNotContain(lineToMove.Id.Value, sourceOrder.OrderLineIds.Select(id => id.Value));
        Assert.Contains(lineToKeep.Id.Value, sourceOrder.OrderLineIds.Select(id => id.Value));
        Assert.Equal(result.OrderId, lineToMove.OrderId.Value);
        Assert.Equal(sourceOrder.Id.Value, lineToKeep.OrderId.Value);
        Assert.Equal("Occupied", targetTable.OccupancyStatus.ToString());
    }

    [Fact]
    public async Task Handle_LineNotOnSourceOrder_Throws()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var tableRepository = new FakeTableRepository();

        var targetTable = Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4);
        tableRepository.Add(targetTable);

        var sourceOrder = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(sourceOrder);

        var unrelatedLine = OrderLine.Create(OrderId.New(), Clovent.Catalog.Variants.ProductVariantId.New(), 1, 5m, 0, false);
        orderLineRepository.Add(unrelatedLine);

        var handler = new SplitOrderCommandHandler(orderRepository, orderLineRepository, tableRepository);

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new SplitOrderCommand(sourceOrder.Id.Value, [unrelatedLine.Id.Value], targetTable.Id.Value), CancellationToken.None));
    }
}
