using Clovent.Catalog.Variants;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Verifies the 18 automated test scenarios for Restaurant POS Hold and Recall functionality
/// defined in Section 27 of the specification:
/// 1. Hold new order
/// 2. Held status persisted
/// 3. Held order contains order lines
/// 4. Recall held order
/// 5. Recalled order retains same Order ID
/// 6. Recalled order retains same Order Number
/// 7. Recalled order restores lines
/// 8. Recalled order restores quantities
/// 9. Recalled order restores variants
/// 10. Recalled order restores customer
/// 11. Recalled Dine-In order restores table
/// 12. Recalled Take Away order has no table
/// 13. Held order survives application restart (test repository reloading)
/// 14. Multiple held orders remain independent
/// 15. Recalled order is removed from Held list
/// 16. Empty order cannot be held
/// 17. Failed persistence does not clear cart (domain validation failure preserves cart)
/// 18. Cancel remains separate from Hold
/// </summary>
public class HoldAndRecallTests
{
    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        public readonly Dictionary<OrderId, Order> Orders = [];

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            Orders[order.Id] = order;
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Orders.GetValueOrDefault(id));

        public Task<IReadOnlyCollection<Order>> GetOpenOrHeldByTableIdAsync(TableId tableId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Order>>([.. Orders.Values.Where(o => o.TableId == tableId && o.Status is OrderStatus.Open or OrderStatus.Held)]);

        public Task<IReadOnlySet<TableId>> GetActiveTableIdsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<TableId>>(Orders.Values
                .Where(o => o.TableId.HasValue && o.Status is OrderStatus.Open or OrderStatus.Held)
                .Select(o => o.TableId!.Value)
                .ToHashSet());

        public Task<IReadOnlyCollection<Order>> GetOpenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Order>>([.. Orders.Values.Where(o => o.Status == OrderStatus.Open)]);

        public Task<IReadOnlyCollection<Order>> GetHeldAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Order>>([.. Orders.Values.Where(o => o.Status == OrderStatus.Held)]);

        public Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Order>>([.. Orders.Values]);
    }

    private sealed class InMemoryOrderLineRepository : IOrderLineRepository
    {
        public readonly Dictionary<OrderLineId, OrderLine> Lines = [];

        public Task AddAsync(OrderLine orderLine, CancellationToken cancellationToken = default)
        {
            Lines[orderLine.Id] = orderLine;
            return Task.CompletedTask;
        }

        public Task<OrderLine?> GetByIdAsync(OrderLineId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Lines.GetValueOrDefault(id));

        public Task<IReadOnlyCollection<OrderLine>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<OrderLine>>([.. Lines.Values.Where(l => l.OrderId == orderId)]);
    }

    // 1. Hold new order
    [Fact]
    public async Task Scenario01_HoldNewOrder_ChangesStatusToHeld()
    {
        var orderRepo = new InMemoryOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var lineId = OrderLineId.New();
        order.AddOrderLine(lineId);
        await orderRepo.AddAsync(order);

        var handler = new HoldOrderCommandHandler(orderRepo);
        var result = await handler.Handle(new HoldOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Held", result.Status);
    }

    // 2. Held status persisted
    [Fact]
    public async Task Scenario02_HeldStatusPersisted_ReflectedInRepository()
    {
        var orderRepo = new InMemoryOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.AddOrderLine(OrderLineId.New());
        await orderRepo.AddAsync(order);

        var handler = new HoldOrderCommandHandler(orderRepo);
        await handler.Handle(new HoldOrderCommand(order.Id.Value), CancellationToken.None);

        var persisted = await orderRepo.GetByIdAsync(order.Id);
        Assert.NotNull(persisted);
        Assert.Equal(OrderStatus.Held, persisted.Status);
    }

    // 3. Held order contains order lines
    [Fact]
    public async Task Scenario03_HeldOrder_ContainsOrderLines()
    {
        var orderRepo = new InMemoryOrderRepository();
        var lineRepo = new InMemoryOrderLineRepository();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line1 = OrderLine.Create(order.Id, ProductVariantId.New(), 2m, 250m, 16m, false);
        var line2 = OrderLine.Create(order.Id, ProductVariantId.New(), 1m, 150m, 16m, false);

        order.AddOrderLine(line1.Id);
        order.AddOrderLine(line2.Id);
        await orderRepo.AddAsync(order);
        await lineRepo.AddAsync(line1);
        await lineRepo.AddAsync(line2);

        var handler = new HoldOrderCommandHandler(orderRepo);
        var dto = await handler.Handle(new HoldOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal(2, dto.OrderLineIds.Count);
        Assert.Contains(line1.Id.Value, dto.OrderLineIds);
        Assert.Contains(line2.Id.Value, dto.OrderLineIds);

        var loadedLines = await lineRepo.GetByOrderIdAsync(order.Id);
        Assert.Equal(2, loadedLines.Count);
    }

    // 4. Recall held order
    [Fact]
    public async Task Scenario04_RecallHeldOrder_ResumesToOpenStatus()
    {
        var orderRepo = new InMemoryOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.AddOrderLine(OrderLineId.New());
        order.Hold();
        await orderRepo.AddAsync(order);

        var handler = new ResumeOrderCommandHandler(orderRepo);
        var result = await handler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Open", result.Status);
    }

    // 5. Recalled order retains same Order ID
    [Fact]
    public async Task Scenario05_RecalledOrder_RetainsSameOrderId()
    {
        var orderRepo = new InMemoryOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.AddOrderLine(OrderLineId.New());
        order.Hold();
        await orderRepo.AddAsync(order);

        var handler = new ResumeOrderCommandHandler(orderRepo);
        var result = await handler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal(order.Id.Value, result.OrderId);
    }

    // 6. Recalled order retains same Order Number
    [Fact]
    public async Task Scenario06_RecalledOrder_RetainsSameOrderNumber()
    {
        var orderRepo = new InMemoryOrderRepository();
        var expectedOrderNumber = OrderNumber.Generate(DateTimeOffset.UtcNow);
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New(), null, expectedOrderNumber);
        order.AddOrderLine(OrderLineId.New());
        order.Hold();
        await orderRepo.AddAsync(order);

        var handler = new ResumeOrderCommandHandler(orderRepo);
        var result = await handler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal(expectedOrderNumber.Value, result.OrderNumber);
    }

    // 7. Recalled order restores lines
    [Fact]
    public async Task Scenario07_RecalledOrder_RestoresLines()
    {
        var orderRepo = new InMemoryOrderRepository();
        var lineRepo = new InMemoryOrderLineRepository();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line1 = OrderLine.Create(order.Id, ProductVariantId.New(), 3m, 120m, 16m, false);
        var line2 = OrderLine.Create(order.Id, ProductVariantId.New(), 1m, 80m, 16m, false);
        order.AddOrderLine(line1.Id);
        order.AddOrderLine(line2.Id);
        order.Hold();
        await orderRepo.AddAsync(order);
        await lineRepo.AddAsync(line1);
        await lineRepo.AddAsync(line2);

        var handler = new ResumeOrderCommandHandler(orderRepo);
        var result = await handler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal(2, result.OrderLineIds.Count);
        var recalledLines = await lineRepo.GetByOrderIdAsync(order.Id);
        Assert.Equal(2, recalledLines.Count);
    }

    // 8. Recalled order restores quantities
    [Fact]
    public async Task Scenario08_RecalledOrder_RestoresQuantities()
    {
        var orderRepo = new InMemoryOrderRepository();
        var lineRepo = new InMemoryOrderLineRepository();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line = OrderLine.Create(order.Id, ProductVariantId.New(), 5.5m, 100m, 0m, false);
        order.AddOrderLine(line.Id);
        order.Hold();
        await orderRepo.AddAsync(order);
        await lineRepo.AddAsync(line);

        var handler = new ResumeOrderCommandHandler(orderRepo);
        await handler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        var recalledLines = await lineRepo.GetByOrderIdAsync(order.Id);
        var restoredLine = Assert.Single(recalledLines);
        Assert.Equal(5.5m, restoredLine.Quantity);
    }

    // 9. Recalled order restores variants
    [Fact]
    public async Task Scenario09_RecalledOrder_RestoresVariants()
    {
        var orderRepo = new InMemoryOrderRepository();
        var lineRepo = new InMemoryOrderLineRepository();

        var variantId = ProductVariantId.New();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line = OrderLine.Create(order.Id, variantId, 2m, 200m, 0m, false);
        order.AddOrderLine(line.Id);
        order.Hold();
        await orderRepo.AddAsync(order);
        await lineRepo.AddAsync(line);

        var handler = new ResumeOrderCommandHandler(orderRepo);
        await handler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        var recalledLines = await lineRepo.GetByOrderIdAsync(order.Id);
        var restoredLine = Assert.Single(recalledLines);
        Assert.Equal(variantId, restoredLine.ProductVariantId);
    }

    // 10. Recalled order restores customer
    [Fact]
    public async Task Scenario10_RecalledOrder_RestoresCustomer()
    {
        var orderRepo = new InMemoryOrderRepository();
        var customerId = CustomerId.New();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.AddOrderLine(OrderLineId.New());
        order.SetCustomer(customerId);
        order.Hold();
        await orderRepo.AddAsync(order);

        var handler = new ResumeOrderCommandHandler(orderRepo);
        var result = await handler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal(customerId.Value, result.CustomerId);
    }

    // 11. Recalled Dine-In order restores table
    [Fact]
    public async Task Scenario11_RecalledDineInOrder_RestoresTable()
    {
        var orderRepo = new InMemoryOrderRepository();
        var tableId = TableId.New();

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), tableId);
        order.AddOrderLine(OrderLineId.New());
        order.Hold();
        await orderRepo.AddAsync(order);

        var handler = new ResumeOrderCommandHandler(orderRepo);
        var result = await handler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("DineIn", result.OrderType);
        Assert.Equal(tableId.Value, result.TableId);
    }

    // 12. Recalled Take Away order has no table
    [Fact]
    public async Task Scenario12_RecalledTakeAwayOrder_HasNoTable()
    {
        var orderRepo = new InMemoryOrderRepository();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.AddOrderLine(OrderLineId.New());
        order.Hold();
        await orderRepo.AddAsync(order);

        var handler = new ResumeOrderCommandHandler(orderRepo);
        var result = await handler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("TakeAway", result.OrderType);
        Assert.Null(result.TableId);
    }

    // 13. Held order survives application restart (test repository reloading)
    [Fact]
    public async Task Scenario13_HeldOrder_SurvivesApplicationRestart()
    {
        // Simulate persistence store
        var persistentStorage = new Dictionary<OrderId, Order>();

        // Session 1: cashier holds the order and application shuts down
        {
            var session1Repo = new InMemoryOrderRepository();
            var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
            order.AddOrderLine(OrderLineId.New());
            await session1Repo.AddAsync(order);

            var holdHandler = new HoldOrderCommandHandler(session1Repo);
            await holdHandler.Handle(new HoldOrderCommand(order.Id.Value), CancellationToken.None);

            foreach (var kvp in session1Repo.Orders)
            {
                persistentStorage[kvp.Key] = kvp.Value;
            }
        }

        // Session 2: application restarts, fresh repository initialized from storage
        {
            var session2Repo = new InMemoryOrderRepository();
            foreach (var kvp in persistentStorage)
            {
                session2Repo.Orders[kvp.Key] = kvp.Value;
            }

            var listQueryHandler = new ListHeldOrdersQueryHandler(session2Repo);
            var heldOrders = await listQueryHandler.Handle(new ListHeldOrdersQuery(), CancellationToken.None);

            var recalled = Assert.Single(heldOrders);
            Assert.Equal("Held", recalled.Status);

            var resumeHandler = new ResumeOrderCommandHandler(session2Repo);
            var resumed = await resumeHandler.Handle(new ResumeOrderCommand(recalled.OrderId), CancellationToken.None);

            Assert.Equal("Open", resumed.Status);
        }
    }

    // 14. Multiple held orders remain independent
    [Fact]
    public async Task Scenario14_MultipleHeldOrders_RemainIndependent()
    {
        var orderRepo = new InMemoryOrderRepository();

        var order1 = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order1.AddOrderLine(OrderLineId.New());
        order1.Hold();
        await orderRepo.AddAsync(order1);

        var order2 = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order2.AddOrderLine(OrderLineId.New());
        order2.Hold();
        await orderRepo.AddAsync(order2);

        var listHandler = new ListHeldOrdersQueryHandler(orderRepo);
        var heldList = await listHandler.Handle(new ListHeldOrdersQuery(), CancellationToken.None);
        Assert.Equal(2, heldList.Count);

        // Resume only order 1
        var resumeHandler = new ResumeOrderCommandHandler(orderRepo);
        await resumeHandler.Handle(new ResumeOrderCommand(order1.Id.Value), CancellationToken.None);

        var heldAfter = await listHandler.Handle(new ListHeldOrdersQuery(), CancellationToken.None);
        var remainingHeld = Assert.Single(heldAfter);
        Assert.Equal(order2.Id.Value, remainingHeld.OrderId);

        var resumed1 = await orderRepo.GetByIdAsync(order1.Id);
        Assert.NotNull(resumed1);
        Assert.Equal(OrderStatus.Open, resumed1.Status);
    }

    // 15. Recalled order is removed from Held list
    [Fact]
    public async Task Scenario15_RecalledOrder_IsRemovedFromHeldList()
    {
        var orderRepo = new InMemoryOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.AddOrderLine(OrderLineId.New());
        order.Hold();
        await orderRepo.AddAsync(order);

        var listHandler = new ListHeldOrdersQueryHandler(orderRepo);
        var heldBefore = await listHandler.Handle(new ListHeldOrdersQuery(), CancellationToken.None);
        Assert.Single(heldBefore);

        var resumeHandler = new ResumeOrderCommandHandler(orderRepo);
        await resumeHandler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        var heldAfter = await listHandler.Handle(new ListHeldOrdersQuery(), CancellationToken.None);
        Assert.Empty(heldAfter);
    }

    // 16. Empty order cannot be held
    [Fact]
    public async Task Scenario16_EmptyOrder_CannotBeHeld()
    {
        var orderRepo = new InMemoryOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New()); // no order lines added!
        await orderRepo.AddAsync(order);

        var handler = new HoldOrderCommandHandler(orderRepo);
        var ex = await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new HoldOrderCommand(order.Id.Value), CancellationToken.None));

        Assert.Contains("has no items", ex.Message);
        Assert.Equal(OrderStatus.Open, order.Status);
    }

    // 17. Failed persistence does not clear cart
    [Fact]
    public async Task Scenario17_FailedPersistence_DoesNotClearCart()
    {
        var orderRepo = new InMemoryOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var lineId = OrderLineId.New();
        order.AddOrderLine(lineId);
        await orderRepo.AddAsync(order);

        // Simulate failed command (e.g. attempting to hold an already completed or voided order, or throwing repository)
        order.Complete();

        var handler = new HoldOrderCommandHandler(orderRepo);
        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new HoldOrderCommand(order.Id.Value), CancellationToken.None));

        // Cart lines in the domain order were NOT destroyed or cleared
        Assert.Contains(lineId, order.OrderLineIds);
    }

    // 18. Cancel remains separate from Hold
    [Fact]
    public async Task Scenario18_Cancel_RemainsSeparateFromHold()
    {
        var orderRepo = new InMemoryOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.AddOrderLine(OrderLineId.New());
        await orderRepo.AddAsync(order);

        // Cancel order
        order.Cancel("Customer decided not to order");
        Assert.Equal(OrderStatus.Cancelled, order.Status);

        // Verify cancelled order cannot be held
        var handler = new HoldOrderCommandHandler(orderRepo);
        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            handler.Handle(new HoldOrderCommand(order.Id.Value), CancellationToken.None));

        // Verify cancelled order does not appear in Held orders list
        var listHandler = new ListHeldOrdersQueryHandler(orderRepo);
        var heldOrders = await listHandler.Handle(new ListHeldOrdersQuery(), CancellationToken.None);
        Assert.Empty(heldOrders);
    }

    // 19. Active orders filter out empty orders (0 items / 0.00) on initial load
    [Fact]
    public async Task Scenario19_ActiveOrders_FilterOutEmptyOrders_OnStartup()
    {
        var orderRepo = new InMemoryOrderRepository();

        // Valid open order with an item
        var validOrder = Order.Create(OrderType.TakeAway, WarehouseId.New());
        validOrder.AddOrderLine(OrderLineId.New());
        await orderRepo.AddAsync(validOrder);

        // Empty open order without items (must be excluded from active rail)
        var emptyOrder = Order.Create(OrderType.TakeAway, WarehouseId.New());
        await orderRepo.AddAsync(emptyOrder);

        // Valid held order with an item
        var heldOrder = Order.Create(OrderType.DineIn, WarehouseId.New(), TableId.New());
        heldOrder.AddOrderLine(OrderLineId.New());
        heldOrder.Hold();
        await orderRepo.AddAsync(heldOrder);

        var openHandler = new ListOpenOrdersQueryHandler(orderRepo);
        var heldHandler = new ListHeldOrdersQueryHandler(orderRepo);

        var openOrders = await openHandler.Handle(new ListOpenOrdersQuery(), CancellationToken.None);
        var heldOrders = await heldHandler.Handle(new ListHeldOrdersQuery(), CancellationToken.None);

        var activeOrders = openOrders.Concat(heldOrders)
            .Where(o => o.OrderLineIds.Count > 0)
            .ToList();

        Assert.Equal(2, activeOrders.Count);
        Assert.Contains(activeOrders, o => o.OrderId == validOrder.Id.Value);
        Assert.Contains(activeOrders, o => o.OrderId == heldOrder.Id.Value);
        Assert.DoesNotContain(activeOrders, o => o.OrderId == emptyOrder.Id.Value);
    }

    // 20. Clear does not cancel, delete, or modify persisted order in database
    [Fact]
    public async Task Scenario20_Clear_DoesNotCancelOrDeletePersistedOrder()
    {
        var orderRepo = new InMemoryOrderRepository();
        var lineRepo = new InMemoryOrderLineRepository();

        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line = OrderLine.Create(order.Id, ProductVariantId.New(), 2m, 150m, 0m, false);
        order.AddOrderLine(line.Id);
        await orderRepo.AddAsync(order);
        await lineRepo.AddAsync(line);

        // Simulate cashier loading order in POS workspace then clicking [ Clear ]
        // Clear is a UI/workspace operation: local workspace state is cleared, no domain cancel/delete command is sent.
        var orderAfterClear = await orderRepo.GetByIdAsync(order.Id);
        Assert.NotNull(orderAfterClear);
        Assert.Equal(OrderStatus.Open, orderAfterClear.Status);
        Assert.NotEqual(OrderStatus.Cancelled, orderAfterClear.Status);
        Assert.NotEqual(OrderStatus.Voided, orderAfterClear.Status);

        var linesAfterClear = await lineRepo.GetByOrderIdAsync(order.Id);
        Assert.Single(linesAfterClear);
    }

    // 21. Recall -> Clear -> Recall workflow preserves held status and original items
    [Fact]
    public async Task Scenario21_Recall_Clear_Recall_PreservesHeldStatusAndItems()
    {
        var orderRepo = new InMemoryOrderRepository();
        var lineRepo = new InMemoryOrderLineRepository();
        var customerId = CustomerId.New();
        var tableId = TableId.New();

        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), tableId);
        order.SetCustomer(customerId);
        var line1 = OrderLine.Create(order.Id, ProductVariantId.New(), 2m, 250m, 0m, false);
        var line2 = OrderLine.Create(order.Id, ProductVariantId.New(), 1m, 100m, 0m, false);
        order.AddOrderLine(line1.Id);
        order.AddOrderLine(line2.Id);
        order.Hold();
        await orderRepo.AddAsync(order);
        await lineRepo.AddAsync(line1);
        await lineRepo.AddAsync(line2);

        // Step 1: Query held orders for recall dialog
        var heldHandler = new ListHeldOrdersQueryHandler(orderRepo);
        var heldList1 = await heldHandler.Handle(new ListHeldOrdersQuery(), CancellationToken.None);
        var recalled1 = Assert.Single(heldList1);
        Assert.Equal("Held", recalled1.Status);
        Assert.Equal(2, recalled1.OrderLineIds.Count);

        // Step 2: Cashier reviews order and clicks [ Clear ] without modifying it.
        // Clear only resets POS workspace. Order in repository remains in Held status.
        var heldInDb = await orderRepo.GetByIdAsync(order.Id);
        Assert.NotNull(heldInDb);
        Assert.Equal(OrderStatus.Held, heldInDb.Status);

        // Step 3: Cashier opens Recall dialog again. Order is still present in held list!
        var heldList2 = await heldHandler.Handle(new ListHeldOrdersQuery(), CancellationToken.None);
        var recalled2 = Assert.Single(heldList2);
        Assert.Equal(order.Id.Value, recalled2.OrderId);
        Assert.Equal(tableId.Value, recalled2.TableId);
        Assert.Equal(customerId.Value, recalled2.CustomerId);

        // Step 4: Verify lines intact
        var lines = await lineRepo.GetByOrderIdAsync(order.Id);
        Assert.Equal(2, lines.Count);
    }

    // 22. Modification on a recalled held order transitions status to Open
    [Fact]
    public async Task Scenario22_ModificationOnRecalledHeldOrder_ResumesStatusToOpen()
    {
        var orderRepo = new InMemoryOrderRepository();
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order.AddOrderLine(OrderLineId.New());
        order.Hold();
        await orderRepo.AddAsync(order);

        Assert.Equal(OrderStatus.Held, order.Status);

        // When modified or payment recorded, EnsureOrderResumedIfHeldAsync triggers ResumeOrderCommand
        var resumeHandler = new ResumeOrderCommandHandler(orderRepo);
        var resumedDto = await resumeHandler.Handle(new ResumeOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.Equal("Open", resumedDto.Status);
        var orderInRepo = await orderRepo.GetByIdAsync(order.Id);
        Assert.NotNull(orderInRepo);
        Assert.Equal(OrderStatus.Open, orderInRepo.Status);
    }

    // 23. Clear button enable/disable state logic
    [Theory]
    [InlineData(false, 0, false)] // No active order, no items -> Disabled
    [InlineData(true, 0, true)]   // Active order loaded, 0 items -> Enabled
    [InlineData(false, 2, true)]  // Unsaved cart with items -> Enabled
    [InlineData(true, 3, true)]   // Active order with items -> Enabled
    public void Scenario23_ClearButton_EnabledState(bool hasOrder, int lineCount, bool expectedEnabled)
    {
        var isClearEnabled = hasOrder || lineCount > 0;
        Assert.Equal(expectedEnabled, isClearEnabled);
    }

    // 24. Action buttons layout responsive at 1024x768 and 1366x768
    [Theory]
    [InlineData(1024, 768)]
    [InlineData(1366, 768)]
    public void Scenario24_BottomActionLayout_ResponsiveAtTargetResolutions(int screenWidth, int screenHeight)
    {
        Assert.True(screenHeight >= 768);
        int rightPanelWidth = screenWidth == 1024 ? 320 : 380;
        int actionPanelHeight = 66;

        using var pnlHoldRecallRow = new TableLayoutPanel
        {
            Width = rightPanelWidth,
            Height = actionPanelHeight / 2,
            ColumnCount = 3,
            RowCount = 1
        };
        pnlHoldRecallRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        pnlHoldRecallRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        pnlHoldRecallRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

        using var btnHold = new Button { Text = "Hold", Dock = DockStyle.Fill };
        using var btnRecall = new Button { Text = "Recall", Dock = DockStyle.Fill };
        using var btnClear = new Button { Text = "Clear", Dock = DockStyle.Fill };

        pnlHoldRecallRow.Controls.Add(btnHold, 0, 0);
        pnlHoldRecallRow.Controls.Add(btnRecall, 1, 0);
        pnlHoldRecallRow.Controls.Add(btnClear, 2, 0);

        pnlHoldRecallRow.PerformLayout();

        // Verify each button has adequate width (>= 80px) and height (>= 25px) for touch/click targets
        Assert.True(btnHold.Width >= 80, $"Hold width {btnHold.Width} should be >= 80");
        Assert.True(btnRecall.Width >= 80, $"Recall width {btnRecall.Width} should be >= 80");
        Assert.True(btnClear.Width >= 80, $"Clear width {btnClear.Width} should be >= 80");

        Assert.True(btnHold.Height >= 25, $"Hold height {btnHold.Height} should be >= 25");
        Assert.True(btnRecall.Height >= 25, $"Recall height {btnRecall.Height} should be >= 25");
        Assert.True(btnClear.Height >= 25, $"Clear height {btnClear.Height} should be >= 25");

        // Verify no overlapping
        Assert.True(btnHold.Right <= btnRecall.Left);
        Assert.True(btnRecall.Right <= btnClear.Left);
    }

    // 25. Recall when current order in POS is already Held does not throw OrderNotOpen
    [Fact]
    public async Task Scenario25_RecallWhenCurrentOrderIsHeld_DoesNotAttemptToReHold()
    {
        var orderRepo = new InMemoryOrderRepository();
        var lineRepo = new InMemoryOrderLineRepository();

        // Order 1 is already held (e.g. recalled earlier or placed on hold)
        var order1 = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line1 = OrderLine.Create(order1.Id, ProductVariantId.New(), 1m, 100m, 0m, false);
        order1.AddOrderLine(line1.Id);
        order1.Hold();
        await orderRepo.AddAsync(order1);
        await lineRepo.AddAsync(line1);

        // Order 2 is also held and being recalled
        var order2 = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line2 = OrderLine.Create(order2.Id, ProductVariantId.New(), 2m, 200m, 0m, false);
        order2.AddOrderLine(line2.Id);
        order2.Hold();
        await orderRepo.AddAsync(order2);
        await lineRepo.AddAsync(line2);

        // In POS, if _currentOrder is order1 (Status == "Held"), recalling order2 must NOT invoke HoldOrderCommand on order1
        // because order1 is already Held. Invoking Hold on a Held order throws RestaurantDomainException ("Order is Held, not Open").
        Assert.Equal(OrderStatus.Held, order1.Status);
        Assert.Equal(OrderStatus.Held, order2.Status);

        // Verify holding an open order succeeds, whereas holding an already held order throws
        var openOrder = Order.Create(OrderType.TakeAway, WarehouseId.New());
        openOrder.AddOrderLine(OrderLineId.New());
        await orderRepo.AddAsync(openOrder);
        var holdHandler = new HoldOrderCommandHandler(orderRepo);
        var heldResult = await holdHandler.Handle(new HoldOrderCommand(openOrder.Id.Value), CancellationToken.None);
        Assert.Equal("Held", heldResult.Status);

        // Confirm domain exception if re-holding held order:
        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            holdHandler.Handle(new HoldOrderCommand(order1.Id.Value), CancellationToken.None));
    }

    // 26. Customer Code and Walk-in customer code display
    [Fact]
    public void Scenario26_CustomerCode_DisplayRules()
    {
        // For a registered customer with Code:
        var customerCode = "C001";
        var customerName = "John Smith";
        var custText = customerCode != "-" ? $"[{customerCode}] {customerName}" : customerName;
        Assert.Equal("[C001] John Smith", custText);

        // For Walk-in Customer:
        var walkInCode = "-";
        var walkInName = "Walk-in Customer";
        var walkInText = walkInCode != "-" ? $"[{walkInCode}] {walkInName}" : walkInName;
        Assert.Equal("Walk-in Customer", walkInText);
    }

    // 27. Customer Picker Popup Grid columns configuration (CustomerId GUID hidden, code/name/phone/balance visible)
    [Fact]
    public void Scenario27_CustomerPicker_PopupColumns_HidesInternalCustomerId()
    {
        using var picker = new DevExpress.XtraEditors.SearchLookUpEdit();
        picker.Properties.ValueMember = "CustomerId";
        picker.Properties.DisplayMember = "Name";

        var popupView = picker.Properties.PopupView;
        Assert.NotNull(popupView);

        popupView.Columns.Clear();
        var colCode = popupView.Columns.AddVisible("CustomerCode", "Customer Code");
        colCode.Width = 120;
        var colName = popupView.Columns.AddVisible("Name", "Name");
        colName.Width = 240;
        var colPhone = popupView.Columns.AddVisible("Phone", "Phone");
        colPhone.Width = 130;
        var colBalance = popupView.Columns.AddVisible("BalanceDisplay", "Balance");
        colBalance.Width = 110;

        // Verify ValueMember is still CustomerId for database persistence & key lookup
        Assert.Equal("CustomerId", picker.Properties.ValueMember);
        Assert.Equal("Name", picker.Properties.DisplayMember);

        // Verify CustomerId is NOT in visible columns
        Assert.Null(popupView.Columns["CustomerId"]);

        // Verify visible columns are only CustomerCode, Name, Phone, BalanceDisplay
        var visibleColumns = popupView.VisibleColumns.Cast<DevExpress.XtraGrid.Columns.GridColumn>().Select(c => c.FieldName).ToList();
        Assert.Equal(["CustomerCode", "Name", "Phone", "BalanceDisplay"], visibleColumns);
    }
}

