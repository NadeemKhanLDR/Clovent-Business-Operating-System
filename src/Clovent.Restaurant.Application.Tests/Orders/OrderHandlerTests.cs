using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Application.Tables.Queries;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.OrderLines;
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
        var handler = new CreateOrderCommandHandler(orderRepository, tableRepository, new FakeOrderNumberSequenceRepository());

        var result = await handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);

        Assert.Equal("Open", result.Status);
        Assert.Equal("Occupied", table.OccupancyStatus.ToString());
    }

    /// <summary>M-3: a free table still opens normally - the guard must not block legitimate dine-in.</summary>
    [Fact]
    public async Task CreateOrderCommandHandler_FreeTable_DineInSucceeds()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        tableRepository.Add(table);
        var handler = new CreateOrderCommandHandler(orderRepository, tableRepository, new FakeOrderNumberSequenceRepository());

        var result = await handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);

        Assert.Equal("Open", result.Status);
        Assert.Single(await orderRepository.GetOpenOrHeldByTableIdAsync(table.Id, CancellationToken.None));
        Assert.Equal("Occupied", table.OccupancyStatus.ToString());
    }

    /// <summary>
    /// M-3: the table already has an Open order, so a second dine-in order is
    /// refused. The table is deliberately left Available, proving the guard
    /// reads the orders rather than leaning on Table.Occupy() throwing - this
    /// is the occupancy-drift case that production table T-01 demonstrates.
    /// </summary>
    [Fact]
    public async Task CreateOrderCommandHandler_TableWithOpenOrder_SecondDineInRejected()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        tableRepository.Add(table);

        var existing = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        orderRepository.Add(existing);

        var handler = new CreateOrderCommandHandler(orderRepository, tableRepository, new FakeOrderNumberSequenceRepository());

        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None));

        Assert.Contains("already has an open or held order", ex.Message);

        // Rejection must not have created a second order...
        var onTable = await orderRepository.GetOpenOrHeldByTableIdAsync(table.Id, CancellationToken.None);
        Assert.Equal(existing.Id, Assert.Single(onTable).Id);

        // ...nor disturbed the table's occupancy.
        Assert.Equal("Available", table.OccupancyStatus.ToString());
    }

    /// <summary>M-3: a Held order holds the table just as an Open one does.</summary>
    [Fact]
    public async Task CreateOrderCommandHandler_TableWithHeldOrder_SecondDineInRejected()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4);
        tableRepository.Add(table);

        var existing = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        existing.Hold();
        orderRepository.Add(existing);

        var handler = new CreateOrderCommandHandler(orderRepository, tableRepository, new FakeOrderNumberSequenceRepository());

        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None));

        Assert.Contains("already has an open or held order", ex.Message);
        Assert.Single(await orderRepository.GetOpenOrHeldByTableIdAsync(table.Id, CancellationToken.None));
        Assert.Equal("Available", table.OccupancyStatus.ToString());
    }

    /// <summary>M-3: a different, free table is unaffected by a busy neighbour.</summary>
    [Fact]
    public async Task CreateOrderCommandHandler_DifferentTable_Succeeds()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var busy = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        var free = Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4);
        tableRepository.Add(busy);
        tableRepository.Add(free);
        orderRepository.Add(Order.Create(OrderType.DineIn, WarehouseId.New(), busy.Id));

        var handler = new CreateOrderCommandHandler(orderRepository, tableRepository, new FakeOrderNumberSequenceRepository());

        var result = await handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, free.Id.Value), CancellationToken.None);

        Assert.Equal("Open", result.Status);
        Assert.Equal(free.Id.Value, result.TableId);
        Assert.Equal("Occupied", free.OccupancyStatus.ToString());
    }

    /// <summary>
    /// M-3: take-away carries no table, so the guard must never fire for it -
    /// including when dine-in orders are already open elsewhere.
    /// </summary>
    [Fact]
    public async Task CreateOrderCommandHandler_TakeAway_UnaffectedByOpenDineInOrders()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        tableRepository.Add(table);
        orderRepository.Add(Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id));

        var handler = new CreateOrderCommandHandler(orderRepository, tableRepository, new FakeOrderNumberSequenceRepository());

        var first = await handler.Handle(new CreateOrderCommand(OrderType.TakeAway, WarehouseId.New().Value), CancellationToken.None);
        var second = await handler.Handle(new CreateOrderCommand(OrderType.TakeAway, WarehouseId.New().Value), CancellationToken.None);

        Assert.Null(first.TableId);
        Assert.Null(second.TableId);
        Assert.Equal("Open", first.Status);
        Assert.Equal("Open", second.Status);
    }

    /// <summary>M-3: once the occupying order is settled, the table opens again.</summary>
    [Fact]
    public async Task CreateOrderCommandHandler_AfterOccupyingOrderCancelled_TableAcceptsNewOrder()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        tableRepository.Add(table);

        var existing = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        orderRepository.Add(existing);

        var handler = new CreateOrderCommandHandler(orderRepository, tableRepository, new FakeOrderNumberSequenceRepository());

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None));

        existing.Cancel("Started by mistake");

        var result = await handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);

        Assert.Equal("Open", result.Status);
        Assert.Single(await orderRepository.GetOpenOrHeldByTableIdAsync(table.Id, CancellationToken.None));
    }

    /// <summary>M-3: a rejected creation must not burn an order number, since the sequence is shared by every later order.</summary>
    [Fact]
    public async Task CreateOrderCommandHandler_RejectedCreation_DoesNotConsumeOrderNumber()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var sequenceRepository = new FakeOrderNumberSequenceRepository();
        var sequence = OrderNumberSequence.CreateDefault();
        sequence.Configure("ORD-", 500);
        sequenceRepository.Add(sequence);

        var busy = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        var free = Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4);
        tableRepository.Add(busy);
        tableRepository.Add(free);
        orderRepository.Add(Order.Create(OrderType.DineIn, WarehouseId.New(), busy.Id));

        var handler = new CreateOrderCommandHandler(orderRepository, tableRepository, sequenceRepository);

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, busy.Id.Value), CancellationToken.None));

        var result = await handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, free.Id.Value), CancellationToken.None);

        Assert.Equal("ORD-500", result.OrderNumber);
    }

    [Fact]
    public async Task CreateOrderCommandHandler_TakeAway_NoTable()
    {
        var handler = new CreateOrderCommandHandler(new FakeOrderRepository(), new FakeTableRepository(), new FakeOrderNumberSequenceRepository());

        var result = await handler.Handle(new CreateOrderCommand(OrderType.TakeAway, WarehouseId.New().Value), CancellationToken.None);

        Assert.Null(result.TableId);
    }

    [Fact]
    public async Task CreateOrderCommandHandler_UsesConfiguredSequence_AndAdvancesIt()
    {
        var sequenceRepository = new FakeOrderNumberSequenceRepository();
        var sequence = OrderNumberSequence.CreateDefault();
        sequence.Configure("INV-", 3453);
        sequenceRepository.Add(sequence);
        var handler = new CreateOrderCommandHandler(new FakeOrderRepository(), new FakeTableRepository(), sequenceRepository);

        var first = await handler.Handle(new CreateOrderCommand(OrderType.TakeAway, WarehouseId.New().Value), CancellationToken.None);
        var second = await handler.Handle(new CreateOrderCommand(OrderType.TakeAway, WarehouseId.New().Value), CancellationToken.None);

        Assert.Equal("INV-3453", first.OrderNumber);
        Assert.Equal("INV-3454", second.OrderNumber);
    }

    [Fact]
    public async Task HoldThenResume_RoundTrips()
    {
        var repository = new FakeOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.AddOrderLine(OrderLineId.New());
        repository.Add(order);

        var held = await new HoldOrderCommandHandler(repository).Handle(new HoldOrderCommand(order.Id.Value), CancellationToken.None);
        Assert.Equal("Held", held.Status);

        var resumed = await new ResumeOrderCommandHandler(repository).Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);
        Assert.Equal("Open", resumed.Status);
    }

    [Fact]
    public async Task HoldOrderCommandHandler_EmptyOrder_ThrowsRestaurantDomainException()
    {
        var repository = new FakeOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        repository.Add(order);

        var handler = new HoldOrderCommandHandler(repository);
        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() => handler.Handle(new HoldOrderCommand(order.Id.Value), CancellationToken.None));
        Assert.Contains("has no items", ex.Message);
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

        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var result = await new VoidOrderCommandHandler(orderRepository, tableRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository).Handle(new VoidOrderCommand(order.Id.Value, "Mistake"), CancellationToken.None);

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
    public async Task CancelOrderCommandHandler_DineIn_ReloadAfterCancellation_PreservesAvailable()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-QA1"), 4);
        table.Occupy();
        tableRepository.Add(table);

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        orderRepository.Add(order);

        // 1. Cancel the Dine-In order
        await new CancelOrderCommandHandler(orderRepository, tableRepository)
            .Handle(new CancelOrderCommand(order.Id.Value, "Customer cancelled"), CancellationToken.None);

        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);

        // 2. Query/reload all tables (simulating closing and reopening the app or reload)
        var listHandler = new ListAllTablesQueryHandler(tableRepository, orderRepository);
        var tables = await listHandler.Handle(new ListAllTablesQuery(), CancellationToken.None);

        var loadedTable = Assert.Single(tables, t => t.TableId == table.Id.Value);
        Assert.Equal("Available", loadedTable.OccupancyStatus);
        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);
    }

    [Fact]
    public async Task CancelOrderCommandHandler_DineIn_KeepsTableOccupied_WhenAnotherActiveOrderExists()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-03"), 4);
        table.Occupy();
        tableRepository.Add(table);

        var order1 = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        var order2 = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        orderRepository.Add(order1);
        orderRepository.Add(order2);

        var result = await new CancelOrderCommandHandler(orderRepository, tableRepository)
            .Handle(new CancelOrderCommand(order1.Id.Value, "Cancel Order 1"), CancellationToken.None);

        Assert.Equal("Cancelled", result.Status);
        Assert.Equal("Occupied", table.OccupancyStatus.ToString());
    }

    [Fact]
    public async Task VoidOrderCommandHandler_DineIn_KeepsTableOccupied_WhenAnotherActiveOrderExists()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-03"), 4);
        table.Occupy();
        tableRepository.Add(table);

        var order1 = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        var order2 = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        orderRepository.Add(order1);
        orderRepository.Add(order2);

        var paymentRepository = new FakePaymentRepository();
        var customerRepository = new FakeCustomerRepository();
        var ledgerRepository = new FakeCustomerLedgerEntryRepository();
        var paymentMethodRepository = new FakePaymentMethodRepository();

        var result = await new VoidOrderCommandHandler(orderRepository, tableRepository, paymentRepository, customerRepository, ledgerRepository, paymentMethodRepository)
            .Handle(new VoidOrderCommand(order1.Id.Value, "Void Order 1"), CancellationToken.None);

        Assert.Equal("Voided", result.Status);
        Assert.Equal("Occupied", table.OccupancyStatus.ToString());
    }

    [Fact]
    public async Task TakeAwayOrder_DoesNotOccupyTable()
    {
        var orderRepository = new FakeOrderRepository();
        var tableRepository = new FakeTableRepository();
        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        tableRepository.Add(table);

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        orderRepository.Add(order);

        Assert.Null(order.TableId);
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
        var open = Order.Create(OrderType.TakeAway, WarehouseId.New());
        open.AddOrderLine(OrderLineId.New());
        repository.Add(open);

        var held = Order.Create(OrderType.TakeAway, WarehouseId.New());
        held.AddOrderLine(OrderLineId.New());
        held.Hold();
        repository.Add(held);

        var result = await new ListOpenOrdersQueryHandler(repository).Handle(new ListOpenOrdersQuery(), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task ListHeldOrdersQueryHandler_FiltersToHeld()
    {
        var repository = new FakeOrderRepository();
        var open = Order.Create(OrderType.TakeAway, WarehouseId.New());
        open.AddOrderLine(OrderLineId.New());
        repository.Add(open);

        var held = Order.Create(OrderType.TakeAway, WarehouseId.New());
        held.AddOrderLine(OrderLineId.New());
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
