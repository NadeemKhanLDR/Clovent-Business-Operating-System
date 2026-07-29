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

public class MergeTablesCommandHandlerTests
{
    [Fact]
    public async Task Handle_TargetHasNoOpenOrder_CreatesOneAndMovesLines()
    {
        var orderRepository = new FakeOrderRepository();
        var orderLineRepository = new FakeOrderLineRepository();
        var tableRepository = new FakeTableRepository();

        var sourceTable = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        sourceTable.Occupy();
        var targetTable = Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4);
        tableRepository.Add(sourceTable);
        tableRepository.Add(targetTable);

        var warehouseId = WarehouseId.New();
        var sourceOrder = Order.Create(OrderType.DineIn, warehouseId, sourceTable.Id);
        var line = OrderLine.Create(sourceOrder.Id, Clovent.Catalog.Variants.ProductVariantId.New(), 2, 5m, 0, false);
        sourceOrder.AddOrderLine(line.Id);
        orderRepository.Add(sourceOrder);
        orderLineRepository.Add(line);

        var handler = new MergeTablesCommandHandler(orderRepository, orderLineRepository, tableRepository);

        var result = await handler.Handle(new MergeTablesCommand(sourceTable.Id.Value, targetTable.Id.Value), CancellationToken.None);

        Assert.Contains(line.Id.Value, result.OrderLineIds);
        Assert.Equal(result.OrderId, line.OrderId.Value);
        Assert.Equal("Cancelled", (await orderRepository.GetByIdAsync(sourceOrder.Id))!.Status.ToString());
        Assert.Equal("Available", sourceTable.OccupancyStatus.ToString());
        Assert.Equal("Occupied", targetTable.OccupancyStatus.ToString());
    }

    [Fact]
    public async Task Handle_SourceHasNoOpenOrder_Throws()
    {
        var tableRepository = new FakeTableRepository();
        var sourceTable = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        var targetTable = Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4);
        tableRepository.Add(sourceTable);
        tableRepository.Add(targetTable);

        var handler = new MergeTablesCommandHandler(new FakeOrderRepository(), new FakeOrderLineRepository(), tableRepository);

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new MergeTablesCommand(sourceTable.Id.Value, targetTable.Id.Value), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SameTable_Throws()
    {
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        tableRepository.Add(table);

        var handler = new MergeTablesCommandHandler(new FakeOrderRepository(), new FakeOrderLineRepository(), tableRepository);

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new MergeTablesCommand(table.Id.Value, table.Id.Value), CancellationToken.None));
    }
}
