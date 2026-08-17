using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Tables.Commands;
using Clovent.Restaurant.Application.Tables.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Tables;

public class TableHandlerTests
{
    [Fact]
    public async Task CreateTableCommandHandler_Valid_Creates()
    {
        var repository = new FakeTableRepository();
        var handler = new CreateTableCommandHandler(repository);
        var diningAreaId = DiningAreaId.New();

        var result = await handler.Handle(new CreateTableCommand(diningAreaId.Value, "T-01", 4), CancellationToken.None);

        Assert.Equal("T-01", result.Code);
        Assert.Equal(4, result.Capacity);
        Assert.Equal("Available", result.OccupancyStatus);
    }

    [Fact]
    public async Task OccupyThenVacate_RoundTrips()
    {
        var repository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        repository.Add(table);

        var occupied = await new OccupyTableCommandHandler(repository).Handle(new OccupyTableCommand(table.Id.Value), CancellationToken.None);
        Assert.Equal("Occupied", occupied.OccupancyStatus);

        var vacated = await new VacateTableCommandHandler(repository, new FakeOrderRepository()).Handle(new VacateTableCommand(table.Id.Value), CancellationToken.None);
        Assert.Equal("Available", vacated.OccupancyStatus);
    }

    /// <summary>
    /// A manual Vacate must refuse a table that still has an <b>Open</b> order
    /// seated at it. This is the T-03 / ORD-54 regression: vacating succeeded
    /// and left a live two-line bill on a table the floor plan showed as free.
    /// </summary>
    [Fact]
    public async Task Vacate_TableWithOpenOrder_IsRefusedAndTableStaysOccupied()
    {
        var tableRepository = new FakeTableRepository();
        var orderRepository = new FakeOrderRepository();

        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-03"), 6);
        table.Occupy();
        tableRepository.Add(table);

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        orderRepository.Add(order);

        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            new VacateTableCommandHandler(tableRepository, orderRepository)
                .Handle(new VacateTableCommand(table.Id.Value), CancellationToken.None));

        Assert.Contains("still has an open or held order", ex.Message);
        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);
    }

    /// <summary>A <b>Held</b> order holds the table just as an Open one does.</summary>
    [Fact]
    public async Task Vacate_TableWithHeldOrder_IsRefusedAndTableStaysOccupied()
    {
        var tableRepository = new FakeTableRepository();
        var orderRepository = new FakeOrderRepository();

        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-03"), 6);
        table.Occupy();
        tableRepository.Add(table);

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        order.Hold();
        orderRepository.Add(order);

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            new VacateTableCommandHandler(tableRepository, orderRepository)
                .Handle(new VacateTableCommand(table.Id.Value), CancellationToken.None));

        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);
    }

    /// <summary>
    /// The orphaned-occupancy case the guard must NOT block - a table flagged
    /// Occupied whose orders have all closed. This is T-01, and freeing it is
    /// the whole point of the manual Vacate action.
    /// </summary>
    [Fact]
    public async Task Vacate_OccupiedTableWhoseOrdersAllClosed_Succeeds()
    {
        var tableRepository = new FakeTableRepository();
        var orderRepository = new FakeOrderRepository();

        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 2);
        table.Occupy();
        tableRepository.Add(table);

        var closed = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        closed.Cancel("Closed earlier");
        orderRepository.Add(closed);

        var result = await new VacateTableCommandHandler(tableRepository, orderRepository)
            .Handle(new VacateTableCommand(table.Id.Value), CancellationToken.None);

        Assert.Equal("Available", result.OccupancyStatus);
        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);
    }

    /// <summary>An order on a <em>different</em> table must not block this one.</summary>
    [Fact]
    public async Task Vacate_LiveOrderOnAnotherTable_DoesNotBlock()
    {
        var tableRepository = new FakeTableRepository();
        var orderRepository = new FakeOrderRepository();

        var target = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 2);
        target.Occupy();
        var other = Table.Create(DiningAreaId.New(), EntityCode.Create("T-03"), 6);
        other.Occupy();
        tableRepository.Add(target);
        tableRepository.Add(other);

        orderRepository.Add(Order.Create(OrderType.DineIn, WarehouseId.New(), other.Id));

        var result = await new VacateTableCommandHandler(tableRepository, orderRepository)
            .Handle(new VacateTableCommand(target.Id.Value), CancellationToken.None);

        Assert.Equal("Available", result.OccupancyStatus);
        Assert.Equal(TableOccupancyStatus.Occupied, other.OccupancyStatus);
    }

    [Fact]
    public async Task Reserve_ThenOccupy_Succeeds()
    {
        var repository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        repository.Add(table);

        await new ReserveTableCommandHandler(repository).Handle(new ReserveTableCommand(table.Id.Value), CancellationToken.None);
        var occupied = await new OccupyTableCommandHandler(repository).Handle(new OccupyTableCommand(table.Id.Value), CancellationToken.None);

        Assert.Equal("Occupied", occupied.OccupancyStatus);
    }

    [Fact]
    public async Task SetOutOfService_ThenReturnToService_RoundTrips()
    {
        var repository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        repository.Add(table);

        var outOfService = await new SetTableOutOfServiceCommandHandler(repository).Handle(new SetTableOutOfServiceCommand(table.Id.Value), CancellationToken.None);
        Assert.Equal("OutOfService", outOfService.OccupancyStatus);

        var returned = await new ReturnTableToServiceCommandHandler(repository).Handle(new ReturnTableToServiceCommand(table.Id.Value), CancellationToken.None);
        Assert.Equal("Available", returned.OccupancyStatus);
    }

    [Fact]
    public async Task SetTableCapacityCommandHandler_Changes()
    {
        var repository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        repository.Add(table);

        var result = await new SetTableCapacityCommandHandler(repository).Handle(new SetTableCapacityCommand(table.Id.Value, 6), CancellationToken.None);

        Assert.Equal(6, result.Capacity);
    }

    [Fact]
    public async Task ActivateThenDeactivate_RoundTrips()
    {
        var repository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        table.Deactivate();
        repository.Add(table);

        var activated = await new ActivateTableCommandHandler(repository).Handle(new ActivateTableCommand(table.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateTableCommandHandler(repository).Handle(new DeactivateTableCommand(table.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task ListTablesByDiningAreaQueryHandler_FiltersToDiningArea()
    {
        var repository = new FakeTableRepository();
        var areaId = DiningAreaId.New();
        repository.Add(Table.Create(areaId, EntityCode.Create("T-01"), 4));
        repository.Add(Table.Create(areaId, EntityCode.Create("T-02"), 4));
        repository.Add(Table.Create(DiningAreaId.New(), EntityCode.Create("T-03"), 4));

        var result = await new ListTablesByDiningAreaQueryHandler(repository).Handle(new ListTablesByDiningAreaQuery(areaId.Value), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ListAllTablesQueryHandler_ReturnsEvery()
    {
        var repository = new FakeTableRepository();
        repository.Add(Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4));
        repository.Add(Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4));

        var result = await new ListAllTablesQueryHandler(repository).Handle(new ListAllTablesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTableByIdQueryHandler_NotFound_Throws()
    {
        var handler = new GetTableByIdQueryHandler(new FakeTableRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetTableByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
