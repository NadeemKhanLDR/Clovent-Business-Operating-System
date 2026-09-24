using Clovent.Catalog.Variants;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Orders;

/// <summary>
/// Business-rule tests for "table selection is Dine-In intent": selecting a
/// valid table must immediately yield a working Dine-In order seated at that
/// table (created through the same CreateOrderCommand the POS table picker
/// now invokes automatically), with no second "Dine In" action required.
/// Covers the command-level guarantees the POS screen relies on:
/// 1. Selecting a valid table sets OrderType = DineIn.
/// 2. Selecting a valid table assigns TableId.
/// 3. Selecting no table does not set DineIn (Take Away stays table-free).
/// 4. A Dine-In order accepts an item immediately after table selection.
/// 5. Hold/Recall preserves DineIn + TableId.
/// 6. Completing the order preserves DineIn + TableId.
/// 7. Changing table before item entry transfers the order - no duplicates.
/// 8. A table already seating a live order is not stolen by a new order.
/// </summary>
public class TableSelectionDineInTests
{
    private sealed class Fixture
    {
        public FakeOrderRepository Orders { get; } = new();
        public FakeTableRepository Tables { get; } = new();
        public FakeOrderNumberSequenceRepository Sequences { get; } = new();
        public FakeOrderLineRepository OrderLines { get; } = new();

        public CreateOrderCommandHandler CreateCreator() => new(Orders, Tables, Sequences);

        public Table AddTable(string code)
        {
            var table = Table.Create(DiningAreaId.New(), EntityCode.Create(code), 4);
            Tables.Add(table);
            return table;
        }

        public Guid AddLine(Order order, decimal unitPrice)
        {
            var variantId = ProductVariantId.New();
            var line = OrderLine.Create(order.Id, variantId, 1, unitPrice, 0, false);
            order.AddOrderLine(line.Id);
            OrderLines.Add(line);
            return variantId.Value;
        }
    }

    // 1 + 2: valid table selection -> DineIn order seated at that table.
    [Fact]
    public async Task TableSelection_CreatesDineInOrder_SeatedAtSelectedTable()
    {
        var fixture = new Fixture();
        var table = fixture.AddTable("T-01");
        var handler = fixture.CreateCreator();

        var order = await handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);

        Assert.Equal("DineIn", order.OrderType);
        Assert.Equal(table.Id.Value, order.TableId);
        Assert.Equal("Open", order.Status);
    }

    // 3: no table -> Take Away, table-free.
    [Fact]
    public async Task TakeAwayCreation_StaysTableFree()
    {
        var fixture = new Fixture();
        var handler = fixture.CreateCreator();

        var order = await handler.Handle(new CreateOrderCommand(OrderType.TakeAway, WarehouseId.New().Value), CancellationToken.None);

        Assert.Equal("TakeAway", order.OrderType);
        Assert.Null(order.TableId);
    }

    // 4: item lands on the Dine-In order immediately after creation - no
    // intermediate state change is required before item entry.
    [Fact]
    public async Task DineInOrder_AcceptsLine_ImmediatelyAfterTableSelection()
    {
        var fixture = new Fixture();
        var table = fixture.AddTable("T-01");
        var dto = await fixture.CreateCreator().Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);

        var persisted = await fixture.Orders.GetByIdAsync(new OrderId(dto.OrderId), CancellationToken.None);
        Assert.NotNull(persisted);
        fixture.AddLine(persisted!, 10m);

        var lines = await fixture.OrderLines.GetByOrderIdAsync(persisted!.Id, CancellationToken.None);
        Assert.Single(lines);
        Assert.Equal("DineIn", dto.OrderType);
        Assert.Equal(table.Id.Value, dto.TableId);
    }

    // 5: Hold then Recall keeps DineIn + TableId.
    [Fact]
    public async Task HoldThenRecall_PreservesDineInAndTable()
    {
        var fixture = new Fixture();
        var table = fixture.AddTable("T-01");
        var dto = await fixture.CreateCreator().Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);
        var persisted = (await fixture.Orders.GetByIdAsync(new OrderId(dto.OrderId), CancellationToken.None))!;
        fixture.AddLine(persisted, 10m);

        var held = await new HoldOrderCommandHandler(fixture.Orders).Handle(new HoldOrderCommand(dto.OrderId), CancellationToken.None);
        Assert.Equal("Held", held.Status);

        var resumed = await new ResumeOrderCommandHandler(fixture.Orders).Handle(new ResumeOrderCommand(dto.OrderId), CancellationToken.None);

        Assert.Equal("Open", resumed.Status);
        Assert.Equal("DineIn", resumed.OrderType);
        Assert.Equal(table.Id.Value, resumed.TableId);
    }

    // 6: completion preserves DineIn + TableId on the persisted order.
    [Fact]
    public async Task CompleteOrder_PreservesDineInAndTable()
    {
        var fixture = new Fixture();
        var table = fixture.AddTable("T-01");
        var dto = await fixture.CreateCreator().Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);
        var persisted = (await fixture.Orders.GetByIdAsync(new OrderId(dto.OrderId), CancellationToken.None))!;
        fixture.AddLine(persisted, 10m);
        var payment = Clovent.Restaurant.Payments.Payment.Create(persisted.Id, Clovent.Restaurant.PaymentMethods.PaymentMethodId.New(), 10m);
        persisted.RecordPayment(payment.Id);

        persisted.Complete();
        var fresh = await fixture.Orders.GetByIdAsync(persisted.Id, CancellationToken.None);

        Assert.Equal(OrderStatus.Completed, fresh!.Status);
        Assert.Equal(OrderType.DineIn, fresh.OrderType);
        Assert.Equal(table.Id, fresh.TableId);
    }

    // 6b: CompleteOrderCommandHandler vacates the seated table to Available and keeps TableId on the order
    [Fact]
    public async Task CompleteOrderCommand_VacatesTable_PreservesTableIdOnOrder()
    {
        var fixture = new Fixture();
        var table = fixture.AddTable("T-01");
        var dto = await fixture.CreateCreator().Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);
        var persisted = (await fixture.Orders.GetByIdAsync(new OrderId(dto.OrderId), CancellationToken.None))!;
        var variantGuid = fixture.AddLine(persisted, 10m);
        var payment = Clovent.Restaurant.Payments.Payment.Create(persisted.Id, Clovent.Restaurant.PaymentMethods.PaymentMethodId.New(), 10m);
        persisted.RecordPayment(payment.Id);

        var paymentsRepo = new FakePaymentRepository();
        paymentsRepo.Add(payment);

        var stockId = Guid.NewGuid();
        var mediator = new FakeMediator(request =>
        {
            object? response = request switch
            {
                Clovent.Inventory.Application.WarehouseStocks.Queries.GetWarehouseStockByWarehouseAndVariantQuery q =>
                    new Clovent.Inventory.Application.WarehouseStocks.Dtos.WarehouseStockDto(
                        stockId, q.WarehouseId, q.ProductVariantId,
                        100, 0, 100, 0, 0, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                Clovent.Inventory.Application.Transactions.Queries.ListInventoryTransactionsByReferenceQuery =>
                    Array.Empty<Clovent.Inventory.Application.Transactions.Dtos.InventoryTransactionDto>(),
                Clovent.Inventory.Application.WarehouseStocks.Commands.IssueStockCommand =>
                    new Clovent.Inventory.Application.WarehouseStocks.Dtos.WarehouseStockDto(
                        stockId, Guid.NewGuid(), variantGuid,
                        99, 0, 99, 0, 0, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                _ => null
            };
            return Task.FromResult<object?>(response);
        });

        var handler = new CompleteOrderCommandHandler(
            fixture.Orders,
            fixture.OrderLines,
            new FakeDiscountRepository(),
            new FakeServiceChargeRepository(),
            paymentsRepo,
            fixture.Tables,
            new FakeDailySalesSequenceRepository(),
            mediator);

        var completed = await handler.Handle(new CompleteOrderCommand(dto.OrderId), CancellationToken.None);

        Assert.Equal("Completed", completed.Status);
        Assert.Equal(table.Id.Value, completed.TableId);
        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);
    }

    // 7: changing table before item entry transfers the single order - no duplicates.
    [Fact]
    public async Task ChangeTableBeforeItemEntry_TransfersOrder_WithoutDuplicate()
    {
        var fixture = new Fixture();
        var t1 = fixture.AddTable("T-01");
        var t2 = fixture.AddTable("T-02");
        var dto = await fixture.CreateCreator().Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, t1.Id.Value), CancellationToken.None);

        var transferred = await new TransferOrderTableCommandHandler(fixture.Orders, fixture.Tables)
            .Handle(new TransferOrderTableCommand(dto.OrderId, t2.Id.Value), CancellationToken.None);

        Assert.Equal("DineIn", transferred.OrderType);
        Assert.Equal(t2.Id.Value, transferred.TableId);
        var all = await fixture.Orders.GetAllAsync(CancellationToken.None);
        Assert.Single(all); // exactly one order - the picker change did not create a second one
    }

    // 8: a table already seating a live order is refused a second order -
    // the POS picker surfaces the seated order instead of creating a new one.
    [Fact]
    public async Task TableWithLiveOrder_SecondDineInRejected()
    {
        var fixture = new Fixture();
        var table = fixture.AddTable("T-01");
        var handler = fixture.CreateCreator();
        await handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None));

        var all = await fixture.Orders.GetAllAsync(CancellationToken.None);
        Assert.Single(all);
    }

    // 9: Empty Dine-In order cancelled -> Table is vacated to Available, TableId preserved on order
    [Fact]
    public async Task CancelEmptyDineInOrder_VacatesTable_PreservesTableIdOnOrder()
    {
        var fixture = new Fixture();
        var table = fixture.AddTable("T-01");
        var creator = fixture.CreateCreator();
        var created = await creator.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);

        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);

        var cancelHandler = new CancelOrderCommandHandler(fixture.Orders, fixture.Tables);
        var cancelled = await cancelHandler.Handle(new CancelOrderCommand(created.OrderId, "Cleared empty draft order"), CancellationToken.None);

        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(table.Id.Value, cancelled.TableId); // Historical TableId preserved!
        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus); // Table vacated!
    }

    // 10: Void Dine-In order -> Table is vacated to Available, TableId preserved on order
    [Fact]
    public async Task VoidDineInOrder_VacatesTable_PreservesTableIdOnOrder()
    {
        var fixture = new Fixture();
        var table = fixture.AddTable("T-01");
        var creator = fixture.CreateCreator();
        var created = await creator.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);

        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);

        var voidHandler = new VoidOrderCommandHandler(
            fixture.Orders,
            fixture.Tables,
            new FakePaymentRepository(),
            new FakeCustomerRepository(),
            new FakeCustomerLedgerEntryRepository(),
            new FakePaymentMethodRepository());

        var voided = await voidHandler.Handle(new VoidOrderCommand(created.OrderId, "Void order"), CancellationToken.None);

        Assert.Equal("Voided", voided.Status);
        Assert.Equal(table.Id.Value, voided.TableId); // Historical TableId preserved!
        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus); // Table vacated!
    }

    // 11: Held Dine-In order continues to occupy the table
    [Fact]
    public async Task HoldDineInOrder_TableRemainsOccupied()
    {
        var fixture = new Fixture();
        var table = fixture.AddTable("T-01");
        var creator = fixture.CreateCreator();
        var created = await creator.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);
        var persisted = (await fixture.Orders.GetByIdAsync(new OrderId(created.OrderId), CancellationToken.None))!;
        fixture.AddLine(persisted, 25m);

        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);

        var held = await new HoldOrderCommandHandler(fixture.Orders).Handle(new HoldOrderCommand(created.OrderId), CancellationToken.None);

        Assert.Equal("Held", held.Status);
        Assert.Equal(table.Id.Value, held.TableId);
        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus); // Table still occupied!
    }

    // 12: Invariant validation helper: Occupied table must have an Open/Held order
    [Fact]
    public async Task InvariantValidation_OccupiedTable_HasOpenOrHeldOrder()
    {
        var fixture = new Fixture();
        var table = fixture.AddTable("T-01");
        var creator = fixture.CreateCreator();

        // 1. Initial state: Available, no orders
        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);
        var activeOrders1 = await fixture.Orders.GetOpenOrHeldByTableIdAsync(table.Id, CancellationToken.None);
        Assert.Empty(activeOrders1);

        // 2. Dine-In created: Occupied, 1 active order
        var created = await creator.Handle(new CreateOrderCommand(OrderType.DineIn, WarehouseId.New().Value, table.Id.Value), CancellationToken.None);
        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);
        var activeOrders2 = await fixture.Orders.GetOpenOrHeldByTableIdAsync(table.Id, CancellationToken.None);
        Assert.Single(activeOrders2);

        // 3. Order cancelled: Available, 0 active orders
        var cancelHandler = new CancelOrderCommandHandler(fixture.Orders, fixture.Tables);
        await cancelHandler.Handle(new CancelOrderCommand(created.OrderId, "Cancel"), CancellationToken.None);
        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);
        var activeOrders3 = await fixture.Orders.GetOpenOrHeldByTableIdAsync(table.Id, CancellationToken.None);
        Assert.Empty(activeOrders3);
    }
}
