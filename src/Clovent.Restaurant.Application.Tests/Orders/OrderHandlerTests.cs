using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Orders;

public class OrderHandlerTests
{
    [Fact]
    public async Task CreateOrderCommandHandler_DineIn_OccupiesTable()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        tableRepository.Add(table);
        var handler = new CreateOrderCommandHandler(orderRepository, tableRepository);

        var result = await handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);

        Assert.Equal("Open", result.Status);
        Assert.Equal("Occupied", table.OccupancyStatus.ToString());
    }

    [Fact]
    public async Task CreateOrderCommandHandler_TakeAway_NoTable()
    {
        var handler = new CreateOrderCommandHandler(new FakeOrderRepository(), new FakeTableRepository());

        var result = await handler.Handle(new CreateOrderCommand(OrderType.TakeAway, WarehouseId.New().Value), CancellationToken.None);

        Assert.Null(result.TableId);
    }

    [Fact]
    public async Task HoldThenResume_RoundTrips()
    {
        var repository = new FakeOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        repository.Add(order);

        var held = await new HoldOrderCommandHandler(repository).Handle(new HoldOrderCommand(order.Id.Value), CancellationToken.None);
        Assert.Equal("Held", held.Status);

        var resumed = await new ResumeOrderCommandHandler(repository).Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);
        Assert.Equal("Open", resumed.Status);
    }

    [Fact]
    public async Task VoidOrderCommandHandler_DineIn_VacatesTable()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        table.Occupy();
        tableRepository.Add(table);
        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        orderRepository.Add(order);

        var result = await new VoidOrderCommandHandler(orderRepository, tableRepository).Handle(new VoidOrderCommand(order.Id.Value, "Mistake"), CancellationToken.None);

        Assert.Equal("Voided", result.Status);
        Assert.Equal("Available", table.OccupancyStatus.ToString());
    }

    [Fact]
    public async Task CancelOrderCommandHandler_DineIn_VacatesTable()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        table.Occupy();
        tableRepository.Add(table);
        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        orderRepository.Add(order);

        var result = await new CancelOrderCommandHandler(orderRepository, tableRepository).Handle(new CancelOrderCommand(order.Id.Value, "Customer left"), CancellationToken.None);

        Assert.Equal("Cancelled", result.Status);
        Assert.Equal("Available", table.OccupancyStatus.ToString());
    }

    [Fact]
    public async Task ReopenOrderCommandHandler_DineIn_ReoccupiesTable()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        table.Occupy();
        tableRepository.Add(table);
        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        order.Cancel("Mistake");
        table.Vacate();
        orderRepository.Add(order);

        var result = await new ReopenOrderCommandHandler(orderRepository, tableRepository).Handle(new ReopenOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Open", result.Status);
        Assert.Equal("Occupied", table.OccupancyStatus.ToString());
    }

    [Fact]
    public async Task SetOrderNotesCommandHandler_SetsNotes()
    {
        var repository = new FakeOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        repository.Add(order);

        var result = await new SetOrderNotesCommandHandler(repository).Handle(new SetOrderNotesCommand(order.Id.Value, "Extra napkins"), CancellationToken.None);

        Assert.Equal("Extra napkins", result.Notes);
    }

    [Fact]
    public async Task SetOrderCustomerNotesCommandHandler_SetsCustomerNotes()
    {
        var repository = new FakeOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        repository.Add(order);

        var result = await new SetOrderCustomerNotesCommandHandler(repository).Handle(new SetOrderCustomerNotesCommand(order.Id.Value, "Birthday"), CancellationToken.None);

        Assert.Equal("Birthday", result.CustomerNotes);
    }

    [Fact]
    public async Task TransferOrderTableCommandHandler_MovesTableAndUpdatesOccupancy()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var oldTable = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        oldTable.Occupy();
        var newTable = Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4);
        tableRepository.Add(oldTable);
        tableRepository.Add(newTable);
        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), oldTable.Id);
        orderRepository.Add(order);

        var result = await new TransferOrderTableCommandHandler(orderRepository, tableRepository)
            .Handle(new TransferOrderTableCommand(order.Id.Value, newTable.Id.Value), CancellationToken.None);

        Assert.Equal(newTable.Id.Value, result.TableId);
        Assert.Equal("Available", oldTable.OccupancyStatus.ToString());
        Assert.Equal("Occupied", newTable.OccupancyStatus.ToString());
    }

    [Fact]
    public async Task ListOpenOrdersQueryHandler_FiltersToOpen()
    {
        var repository = new FakeOrderRepository();
        repository.Add(Order.Create(OrderType.TakeAway, WarehouseId.New()));
        var held = Order.Create(OrderType.TakeAway, WarehouseId.New());
        held.Hold();
        repository.Add(held);

        var result = await new ListOpenOrdersQueryHandler(repository).Handle(new ListOpenOrdersQuery(), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task ListHeldOrdersQueryHandler_FiltersToHeld()
    {
        var repository = new FakeOrderRepository();
        repository.Add(Order.Create(OrderType.TakeAway, WarehouseId.New()));
        var held = Order.Create(OrderType.TakeAway, WarehouseId.New());
        held.Hold();
        repository.Add(held);

        var result = await new ListHeldOrdersQueryHandler(repository).Handle(new ListHeldOrdersQuery(), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetOrderByIdQueryHandler_NotFound_Throws()
    {
        var handler = new GetOrderByIdQueryHandler(new FakeOrderRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
