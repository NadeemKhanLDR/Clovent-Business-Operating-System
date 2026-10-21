using System;

using System.Collections.Generic;

using System.Linq;

using System.Text.Json;

using System.Threading;

using System.Threading.Tasks;

using Clovent.Catalog.Variants;

using Clovent.Identity.Branches;

using Clovent.Identity.Users;

using Clovent.Inventory.Application.Transactions.Queries;

using Clovent.Inventory.Application.WarehouseStocks.Commands;

using Clovent.Inventory.Application.WarehouseStocks.Queries;

using Clovent.Inventory.Infrastructure.Persistence;

using Clovent.Inventory.Transactions;

using Clovent.MasterData.Terminals;

using Clovent.MasterData.Warehouses;

using Clovent.Restaurant.Application.Orders.Commands;

using Clovent.Restaurant.Application.Outbox;

using Clovent.Restaurant.Application.Outbox.Dtos;

using Clovent.Restaurant.Application.Outbox.Handlers;

using Clovent.Restaurant.Application.Refunds.Commands;

using Clovent.Restaurant.Application.Refunds.Dtos;

using Clovent.Restaurant.Infrastructure.Persistence;

using Clovent.Restaurant.OrderLines;

using Clovent.Restaurant.Orders;

using Clovent.Restaurant.Orders.ValueObjects;

using Clovent.Restaurant.Outbox;

using Clovent.Restaurant.PaymentMethods;

using Clovent.Restaurant.PaymentMethods.ValueObjects;

using Clovent.Restaurant.Payments;

using Clovent.Restaurant.Refunds;

using Clovent.Restaurant.Shifts;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Infrastructure;

using Microsoft.EntityFrameworkCore.Storage;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Outbox;

/// <summary>

/// End-to-end integration test suite verifying inventory consistency between sales and refunds.

/// Covers:

/// 1. Normal sale followed by partial return (net deduction verified).

/// 2. Insufficient stock on sale (AllowNegativeStock = false) routing to DeadLetter,

///    followed by refund (no restock inflation, financial refund succeeds).

/// 3. Untracked products (sale and refund processed without creating phantom WarehouseStock or OpeningBalance rows).

/// 4. Refund processed before sale posting completes (out-of-order arrival: restock deferred/retried until sale completes).

/// 5. Retries and duplicate delivery (at-least-once outbox message delivery yields exactly-once ledger movements).

/// 6. Concurrent processing (simultaneous sale and refund executions resolve deterministically).

/// </summary>

public sealed class InventorySaleRefundConsistencyIntegrationTests : IAsyncLifetime

{

    private readonly string _databaseName;

    private readonly string _connectionString;

    private readonly ServiceProvider _serviceProvider;

    private readonly BranchId _branchId = new(Guid.NewGuid());

    private readonly WarehouseId _warehouseId = new(Guid.NewGuid());

    private readonly TerminalId _terminalId = new(Guid.NewGuid());

    private readonly UserId _cashierId = new(Guid.NewGuid());

    private Guid _shiftId;

    private Guid _cashPaymentMethodId;

    public InventorySaleRefundConsistencyIntegrationTests()

    {

        _databaseName = $"CBOS_InvConsistency_{Guid.NewGuid():N}";

        _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30;";

        var configuration = new ConfigurationBuilder()

            .AddInMemoryCollection(new Dictionary<string, string?>

            {

                ["ConnectionStrings:Default"] = _connectionString,

                ["ConnectionStrings:Restaurant"] = _connectionString,

                ["ConnectionStrings:Inventory"] = _connectionString

            })

            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        services.AddSingleton<Clovent.Platform.CircuitBreakers.ICircuitBreakerRegistry, Clovent.Platform.CircuitBreakers.CircuitBreakerRegistry>();

        Clovent.Platform.Sync.SyncServiceCollectionExtensions.AddTestPlatformSync(services);

        Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(services, configuration);

        Clovent.Restaurant.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(services, configuration);

        Clovent.Restaurant.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(services, configuration);

        Clovent.Inventory.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(services, configuration);

        Clovent.Inventory.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(services, configuration);

        Clovent.Inventory.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(services, configuration);

        _serviceProvider = services.BuildServiceProvider();

    }

    public async Task InitializeAsync()

    {

        using var initScope = _serviceProvider.CreateScope();

        var restaurantDb = initScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

        var inventoryDb = initScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        await restaurantDb.Database.EnsureCreatedAsync();

        var inventoryCreator = (IRelationalDatabaseCreator)inventoryDb.Database.GetService<IDatabaseCreator>();

        await inventoryCreator.CreateTablesAsync();

        // Seed PaymentMethod

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));

        await restaurantDb.PaymentMethods.AddAsync(cashMethod);

        // Seed Shift

        var shift = Shift.Open(

            shiftNumber: 201,

            branchId: _branchId,

            warehouseId: _warehouseId,

            terminalId: _terminalId,

            cashierId: _cashierId,

            cashierName: "Cashier Inventory Tester",

            startingCash: 5000m);

        await restaurantDb.Shifts.AddAsync(shift);

        await restaurantDb.SaveChangesAsync();

        _shiftId = shift.Id.Value;

        _cashPaymentMethodId = cashMethod.Id.Value;

    }

    public async Task DisposeAsync()

    {

        try

        {

            using var cleanupScope = _serviceProvider.CreateScope();

            var restaurantDb = cleanupScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

            await restaurantDb.Database.EnsureDeletedAsync();

        }

        catch

        {

            // Suppress cleanup failure in tests

        }

        await _serviceProvider.DisposeAsync();

    }

    private async Task<ProductVariantId> SeedTrackedStockAsync(decimal initialQuantity)

    {

        var variantId = new ProductVariantId(Guid.NewGuid());

        using var scope = _serviceProvider.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        if (initialQuantity > 0)

        {

            await mediator.Send(new OpenOrReceiveStockCommand(

                _warehouseId.Value,

                variantId.Value,

                initialQuantity,

                "Initial Seed Stock",

                "Seed",

                Guid.NewGuid()));

        }

        else

        {

            // Create WarehouseStock record with 0 quantity on hand (AllowNegativeStock = false)

            var inventoryDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            var stock = Clovent.Inventory.WarehouseStocks.WarehouseStock.Create(

                _warehouseId,

                variantId,

                minimumStock: 0m,

                maximumStock: 0m,

                allowNegativeStock: false);

            await inventoryDb.WarehouseStocks.AddAsync(stock);

            await inventoryDb.SaveChangesAsync();

        }

        return variantId;

    }

    private async Task<(Guid OrderId, Guid OrderLineId)> CreateAndCompleteOrderAsync(

        ProductVariantId variantId,

        decimal quantity,

        decimal unitPrice = 500m)

    {

        using var scope = _serviceProvider.CreateScope();

        var restaurantDb = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

        var order = Order.Create(OrderType.TakeAway, _warehouseId, null, OrderNumber.Generate(DateTimeOffset.UtcNow));

        var orderLine = OrderLine.Create(order.Id, variantId, quantity, unitPrice, 16m, false);

        var subtotal = quantity * unitPrice;

        var tax = Math.Round(subtotal * 0.16m, 2, MidpointRounding.AwayFromZero);

        orderLine.SetTaxSnapshot("Taxable", "PRA", "PK-PRA-16", subtotal, 0m, tax, "1.3.0-AwayFromZero-v1");

        order.AddOrderLine(orderLine.Id);

        var payment = Payment.Create(order.Id, new PaymentMethodId(_cashPaymentMethodId), subtotal + tax, new ShiftId(_shiftId));

        order.RecordPayment(payment.Id);

        order.AssignDailySalesNumber(1);

        order.Complete();

        // Generate the standard InventoryPosting outbox message as CompleteOrderCommand does

        var activeItems = new List<InventoryPostingLineItem>

        {

            new(variantId.Value, $"SKU-{variantId.Value:N}", "Test Product", quantity)

        };

        var invPayload = new InventoryPostingPayload(

            order.Id.Value,

            order.OrderNumber.Value,

            order.WarehouseId.Value,

            activeItems);

        var invMessage = OutboxMessage.Create(

            OutboxMessageType.InventoryPosting,

            "Order",

            order.Id.Value.ToString(),

            order.Id.Value.ToString(),

            JsonSerializer.Serialize(invPayload),

            $"inv-{order.Id.Value}");

        await restaurantDb.Orders.AddAsync(order);

        await restaurantDb.OrderLines.AddAsync(orderLine);

        await restaurantDb.Payments.AddAsync(payment);

        await restaurantDb.OutboxMessages.AddAsync(invMessage);

        await restaurantDb.SaveChangesAsync();

        return (order.Id.Value, orderLine.Id.Value);

    }

    // =================================================================================================

    // 1. Normal Sale Followed by Partial Return

    // =================================================================================================

    [Fact]

    public async Task Scenario1_NormalSale_FollowedByPartialReturn_ProducesAccurateNetInventoryDeduction()

    {

        // Arrange: 10 units initially on hand

        var variantId = await SeedTrackedStockAsync(10m);

        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(variantId, quantity: 3m);

        // Process sale outbox message

        var processor = _serviceProvider.GetRequiredService<IOutboxProcessor>();

        var processedSales = await processor.ProcessBatchAsync();

        Assert.True(processedSales > 0);

        // Verify stock drops from 10 to 7

        using (var verifyScope = _serviceProvider.CreateScope())

        {

            var mediator = verifyScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(7m, stock!.QuantityOnHand);

        }

        // Act: Process refund for 1 unit with restock

        Guid refundId;

        using (var refundScope = _serviceProvider.CreateScope())

        {

            var mediator = refundScope.ServiceProvider.GetRequiredService<IMediator>();

            var refundCmd = new ProcessRefundCommand(

                OrderId: orderId,

                Items: [new RefundItemRequest(orderLineId, 1m, InventoryDisposition.Restock)],

                Reason: "Customer Partial Return",

                SettlementMethod: RefundSettlementMethod.CashPayout,

                IdempotencyKey: $"REF-SCENARIO-1-{Guid.NewGuid():N}",

                CashierId: _cashierId.Value,

                CashierName: "Cashier Alice",

                BranchId: _branchId.Value,

                ShiftId: _shiftId,

                TerminalId: _terminalId.Value);

            var refundDto = await mediator.Send(refundCmd);

            Assert.NotNull(refundDto);

            refundId = refundDto.RefundId;

        }

        // Process restock outbox message

        var processedRefunds = await processor.ProcessBatchAsync();

        Assert.True(processedRefunds > 0);

        // Assert: Net stock is exactly 8 (10 - 3 + 1 = 8)

        using (var finalScope = _serviceProvider.CreateScope())

        {

            var mediator = finalScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(8m, stock!.QuantityOnHand);

            // Verify immutable ledger transactions: exactly 1 Issue (3) and 1 Receipt (1)

            var orderTxs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Order", orderId));

            Assert.Single(orderTxs, t => t.TransactionType == nameof(InventoryTransactionType.Issue) && t.Quantity == 3m);

            var refundTxs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", refundId));

            var variantRefundTxs = refundTxs.Where(t => t.ProductVariantId == variantId.Value).ToList();

            Assert.Single(variantRefundTxs);

            Assert.Equal(nameof(InventoryTransactionType.Receipt), variantRefundTxs[0].TransactionType);

            Assert.Equal(1m, variantRefundTxs[0].Quantity);

        }

    }

    // =================================================================================================

    // 2. Insufficient Stock on Sale Routing to DeadLetter, Followed by Refund

    // =================================================================================================

    [Fact]

    public async Task Scenario2_InsufficientStock_SaleFailsToDeadLetter_RefundDoesNotInflateInventory_AndFinancialRefundSucceeds()

    {

        // Arrange: 0 units on hand and AllowNegativeStock = false (Attempt 4 PAK-ROTI-EXEMPT scenario)

        var variantId = await SeedTrackedStockAsync(0m);

        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(variantId, quantity: 1m);

        // Act: Attempt to process sale outbox message

        using (var scope = _serviceProvider.CreateScope())

        {

            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var saleMsg = await outboxRepo.GetByIdempotencyKeyAsync($"inv-{orderId}");

            Assert.NotNull(saleMsg);

            var handler = scope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            // Handler throws InsufficientStockException because AllowNegativeStock is false

            await Assert.ThrowsAnyAsync<Exception>(() => handler.HandleAsync(saleMsg!, CancellationToken.None));

            // Mark dead letter to simulate reaching retry limit

            saleMsg!.MarkDeadLetter("Insufficient stock on hand");

            await outboxRepo.UpdateAsync(saleMsg, CancellationToken.None);

        }

        // Verify stock is still 0 and no Issue was recorded

        using (var verifyScope = _serviceProvider.CreateScope())

        {

            var mediator = verifyScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(0m, stock!.QuantityOnHand);

            var orderTxs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Order", orderId));

            Assert.Empty(orderTxs);

        }

        // Financial refund for the customer MUST succeed legitimately

        Guid refundId;

        using (var refundScope = _serviceProvider.CreateScope())

        {

            var mediator = refundScope.ServiceProvider.GetRequiredService<IMediator>();

            var refundCmd = new ProcessRefundCommand(

                OrderId: orderId,

                Items: [new RefundItemRequest(orderLineId, 1m, InventoryDisposition.Restock)],

                Reason: "Defective item return",

                SettlementMethod: RefundSettlementMethod.CashPayout,

                IdempotencyKey: $"REF-SCENARIO-2-{Guid.NewGuid():N}",

                CashierId: _cashierId.Value,

                CashierName: "Cashier Alice",

                BranchId: _branchId.Value,

                ShiftId: _shiftId,

                TerminalId: _terminalId.Value);

            var refundDto = await mediator.Send(refundCmd);

            Assert.NotNull(refundDto);

            refundId = refundDto.RefundId;

        }

        // Process restock outbox message

        using (var restockScope = _serviceProvider.CreateScope())

        {

            var outboxRepo = restockScope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var restockMsg = await outboxRepo.GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");

            Assert.NotNull(restockMsg);

            var handler = restockScope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            // Handler throws InvalidOperationException quarantining the restock because depletion evidence is missing (totalIssuedForOrder == 0)

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(

                () => handler.HandleAsync(restockMsg!, CancellationToken.None));

            Assert.Contains("quarantined", ex.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Contains("no sale depletion evidence", ex.Message, StringComparison.OrdinalIgnoreCase);

            restockMsg!.MarkDeadLetter(ex.Message);

            await outboxRepo.UpdateAsync(restockMsg, CancellationToken.None);

        }

        // Assert: Stock is STILL 0 (NO inventory inflation / fabricated stock)

        using (var finalScope = _serviceProvider.CreateScope())

        {

            var mediator = finalScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(0m, stock!.QuantityOnHand);

            // No Receipt transactions fabricated for this refund

            var refundTxs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", refundId));

            Assert.Empty(refundTxs);

        }

    }

    // =================================================================================================

    // 3. Untracked Products (No Phantom Rows on Sale or Refund)

    // =================================================================================================

    [Fact]

    public async Task Scenario3_UntrackedProduct_SaleAndRefund_LeavesStockUntrackedWithoutPhantomRows()

    {

        // Variant is created WITHOUT any WarehouseStock row at this warehouse (untracked item)

        var untrackedVariantId = new ProductVariantId(Guid.NewGuid());

        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(untrackedVariantId, quantity: 2m);

        // Process sale outbox message

        var processor = _serviceProvider.GetRequiredService<IOutboxProcessor>();

        var salesProcessed = await processor.ProcessBatchAsync();

        Assert.True(salesProcessed > 0);

        // Verify NO WarehouseStock row was created

        using (var checkScope = _serviceProvider.CreateScope())

        {

            var mediator = checkScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, untrackedVariantId.Value));

            Assert.Null(stock);

        }

        // Act: Process refund with Restock disposition

        Guid refundId;

        using (var refundScope = _serviceProvider.CreateScope())

        {

            var mediator = refundScope.ServiceProvider.GetRequiredService<IMediator>();

            var refundCmd = new ProcessRefundCommand(

                OrderId: orderId,

                Items: [new RefundItemRequest(orderLineId, 1m, InventoryDisposition.Restock)],

                Reason: "Untracked item return",

                SettlementMethod: RefundSettlementMethod.CashPayout,

                IdempotencyKey: $"REF-SCENARIO-3-{Guid.NewGuid():N}",

                CashierId: _cashierId.Value,

                CashierName: "Cashier Alice",

                BranchId: _branchId.Value,

                ShiftId: _shiftId,

                TerminalId: _terminalId.Value);

            var refundDto = await mediator.Send(refundCmd);

            Assert.NotNull(refundDto);

            refundId = refundDto.RefundId;

        }

        // Process restock outbox message

        var refundsProcessed = await processor.ProcessBatchAsync();

        Assert.True(refundsProcessed > 0);

        // Assert: Still NO WarehouseStock row exists (untracked remains untracked, NO phantom OpeningBalance)

        using (var finalScope = _serviceProvider.CreateScope())

        {

            var mediator = finalScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, untrackedVariantId.Value));

            Assert.Null(stock);

            var inventoryDb = finalScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            var anyStock = await inventoryDb.WarehouseStocks

                .AnyAsync(s => s.WarehouseId == _warehouseId && s.ProductVariantId == untrackedVariantId);

            Assert.False(anyStock);

            var anyTx = await inventoryDb.InventoryTransactions

                .AnyAsync(t => t.ProductVariantId == untrackedVariantId);

            Assert.False(anyTx);

        }

    }

    // =================================================================================================

    // 4. Refund Processed Before Sale Posting Completes (Out-of-Order Message Arrival)

    // =================================================================================================

    [Fact]

    public async Task Scenario4_RefundBeforeSalePosting_OutOfOrderArrival_DefersRestockUntilSaleCompletes_ThenAccurateNetStock()

    {

        // Tracked stock with 10 units

        var variantId = await SeedTrackedStockAsync(10m);

        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(variantId, quantity: 2m);

        // At this point, sale outbox message is Pending in database

        Guid refundId;

        using (var refundScope = _serviceProvider.CreateScope())

        {

            var mediator = refundScope.ServiceProvider.GetRequiredService<IMediator>();

            var refundCmd = new ProcessRefundCommand(

                OrderId: orderId,

                Items: [new RefundItemRequest(orderLineId, 1m, InventoryDisposition.Restock)],

                Reason: "Out of order test",

                SettlementMethod: RefundSettlementMethod.CashPayout,

                IdempotencyKey: $"REF-SCENARIO-4-{Guid.NewGuid():N}",

                CashierId: _cashierId.Value,

                CashierName: "Cashier Alice",

                BranchId: _branchId.Value,

                ShiftId: _shiftId,

                TerminalId: _terminalId.Value);

            var refundDto = await mediator.Send(refundCmd);

            Assert.NotNull(refundDto);

            refundId = refundDto.RefundId;

        }

        // Now: simulate restock outbox message arriving BEFORE sale outbox message

        using (var outOfOrderScope = _serviceProvider.CreateScope())

        {

            var outboxRepo = outOfOrderScope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var restockMsg = await outboxRepo.GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");

            Assert.NotNull(restockMsg);

            var handler = outOfOrderScope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            // Handler MUST throw InvalidOperationException indicating original sale posting is Pending,

            // deferring the restock until the sale is completed.

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(

                () => handler.HandleAsync(restockMsg!, CancellationToken.None));

            Assert.Contains("is currently Pending. Deferring restock retry", ex.Message);

        }

        // Verify stock is still 10 (no premature restock)

        using (var midScope = _serviceProvider.CreateScope())

        {

            var mediator = midScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(10m, stock!.QuantityOnHand);

        }

        // Now process the original sale outbox message

        using (var saleScope = _serviceProvider.CreateScope())

        {

            var outboxRepo = saleScope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var saleMsg = await outboxRepo.GetByIdempotencyKeyAsync($"inv-{orderId}");

            Assert.NotNull(saleMsg);

            var handler = saleScope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            await handler.HandleAsync(saleMsg!, CancellationToken.None);

            saleMsg!.MarkCompleted(DateTimeOffset.UtcNow);

            await outboxRepo.UpdateAsync(saleMsg, CancellationToken.None);

        }

        // Verify stock dropped to 8 after sale completed

        using (var midScope2 = _serviceProvider.CreateScope())

        {

            var mediator = midScope2.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(8m, stock!.QuantityOnHand);

        }

        // Now retry the deferred restock outbox message

        using (var retryScope = _serviceProvider.CreateScope())

        {

            var outboxRepo = retryScope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var restockMsg = await outboxRepo.GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");

            Assert.NotNull(restockMsg);

            var handler = retryScope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            // Now that original sale has completed and totalIssuedForOrder is 2, restock succeeds!

            await handler.HandleAsync(restockMsg!, CancellationToken.None);

            restockMsg!.MarkCompleted(DateTimeOffset.UtcNow);

            await outboxRepo.UpdateAsync(restockMsg, CancellationToken.None);

        }

        // Assert: Net stock is exactly 9 (10 - 2 + 1 = 9)

        using (var finalScope = _serviceProvider.CreateScope())

        {

            var mediator = finalScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(9m, stock!.QuantityOnHand);

        }

    }

    // =================================================================================================

    // 5. Retries and Duplicate Delivery (At-Least-Once Delivery Guarantees Exactly-Once Movements)

    // =================================================================================================

    [Fact]

    public async Task Scenario5_OutboxRetriesAndDuplicateDeliveries_YieldsIdempotentMovements_ZeroDuplicateDeductionsOrReceipts()

    {

        var variantId = await SeedTrackedStockAsync(10m);

        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(variantId, quantity: 2m);

        // Deliver sale outbox message TWICE directly to handler

        using (var duplicateSaleScope = _serviceProvider.CreateScope())

        {

            var outboxRepo = duplicateSaleScope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var saleMsg = await outboxRepo.GetByIdempotencyKeyAsync($"inv-{orderId}");

            Assert.NotNull(saleMsg);

            var handler = duplicateSaleScope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            // First delivery

            await handler.HandleAsync(saleMsg!, CancellationToken.None);

            // Second delivery (duplicate delivery / at-least-once redelivery)

            await handler.HandleAsync(saleMsg!, CancellationToken.None);

            saleMsg!.MarkCompleted(DateTimeOffset.UtcNow);

            await outboxRepo.UpdateAsync(saleMsg, CancellationToken.None);

        }

        // Verify: stock dropped by exactly 2 (to 8), NOT 4!

        using (var verifyScope = _serviceProvider.CreateScope())

        {

            var mediator = verifyScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(8m, stock!.QuantityOnHand);

            var orderTxs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Order", orderId));

            Assert.Single(orderTxs);

            Assert.Equal(2m, orderTxs.First().Quantity);

        }

        // Process refund for 1 unit

        Guid refundId;

        using (var refundScope = _serviceProvider.CreateScope())

        {

            var mediator = refundScope.ServiceProvider.GetRequiredService<IMediator>();

            var refundCmd = new ProcessRefundCommand(

                OrderId: orderId,

                Items: [new RefundItemRequest(orderLineId, 1m, InventoryDisposition.Restock)],

                Reason: "Duplicate delivery test",

                SettlementMethod: RefundSettlementMethod.CashPayout,

                IdempotencyKey: $"REF-SCENARIO-5-{Guid.NewGuid():N}",

                CashierId: _cashierId.Value,

                CashierName: "Cashier Alice",

                BranchId: _branchId.Value,

                ShiftId: _shiftId,

                TerminalId: _terminalId.Value);

            var refundDto = await mediator.Send(refundCmd);

            Assert.NotNull(refundDto);

            refundId = refundDto.RefundId;

        }

        // Deliver restock outbox message TWICE directly to handler

        using (var duplicateRestockScope = _serviceProvider.CreateScope())

        {

            var outboxRepo = duplicateRestockScope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var restockMsg = await outboxRepo.GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");

            Assert.NotNull(restockMsg);

            var handler = duplicateRestockScope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            // First delivery

            await handler.HandleAsync(restockMsg!, CancellationToken.None);

            // Second delivery (duplicate delivery / at-least-once redelivery)

            await handler.HandleAsync(restockMsg!, CancellationToken.None);

            restockMsg!.MarkCompleted(DateTimeOffset.UtcNow);

            await outboxRepo.UpdateAsync(restockMsg, CancellationToken.None);

        }

        // Verify: stock increased by exactly 1 (to 9), NOT 10!

        using (var finalScope = _serviceProvider.CreateScope())

        {

            var mediator = finalScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(9m, stock!.QuantityOnHand);

            var refundTxs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", refundId));

            Assert.Single(refundTxs);

            Assert.Equal(1m, refundTxs.First().Quantity);

        }

    }

    // =================================================================================================

    // 6. Concurrent Processing (Simultaneous Outbox Message Processing)

    // =================================================================================================

    [Fact]

    public async Task Scenario6_ConcurrentProcessing_SaleAndRefundSimultaneous_ResolvesDeterministicallyWithoutRaceConditions()

    {

        var variantId = await SeedTrackedStockAsync(10m);

        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(variantId, quantity: 2m);

        Guid refundId;

        using (var refundScope = _serviceProvider.CreateScope())

        {

            var mediator = refundScope.ServiceProvider.GetRequiredService<IMediator>();

            var refundCmd = new ProcessRefundCommand(

                OrderId: orderId,

                Items: [new RefundItemRequest(orderLineId, 1m, InventoryDisposition.Restock)],

                Reason: "Concurrent processing test",

                SettlementMethod: RefundSettlementMethod.CashPayout,

                IdempotencyKey: $"REF-SCENARIO-6-{Guid.NewGuid():N}",

                CashierId: _cashierId.Value,

                CashierName: "Cashier Alice",

                BranchId: _branchId.Value,

                ShiftId: _shiftId,

                TerminalId: _terminalId.Value);

            var refundDto = await mediator.Send(refundCmd);

            refundId = refundDto.RefundId;

        }

        // Run both outbox messages concurrently in separate scopes

        using (var scope1 = _serviceProvider.CreateScope())

        using (var scope2 = _serviceProvider.CreateScope())

        {

            var outboxRepo1 = scope1.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var outboxRepo2 = scope2.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var saleMsg = await outboxRepo1.GetByIdempotencyKeyAsync($"inv-{orderId}");

            var restockMsg = await outboxRepo2.GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");

            Assert.NotNull(saleMsg);

            Assert.NotNull(restockMsg);

            var handler1 = scope1.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            var handler2 = scope2.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            var taskSale = Task.Run(async () =>

            {

                await handler1.HandleAsync(saleMsg!, CancellationToken.None);

                saleMsg!.MarkCompleted(DateTimeOffset.UtcNow);

                await outboxRepo1.UpdateAsync(saleMsg, CancellationToken.None);

            });

            var taskRestock = Task.Run(async () =>

            {

                try

                {

                    await handler2.HandleAsync(restockMsg!, CancellationToken.None);

                    restockMsg!.MarkCompleted(DateTimeOffset.UtcNow);

                    await outboxRepo2.UpdateAsync(restockMsg, CancellationToken.None);

                }

                catch (InvalidOperationException)

                {

                    // If restock raced ahead of sale, it threw deferral exception

                    // Wait for sale to complete, then retry restock

                    await taskSale;

                    using var retryScope = _serviceProvider.CreateScope();

                    var retryHandler = retryScope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                        .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

                    var retryRepo = retryScope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                    var freshRestockMsg = await retryRepo.GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");

                    await retryHandler.HandleAsync(freshRestockMsg!, CancellationToken.None);

                    freshRestockMsg!.MarkCompleted(DateTimeOffset.UtcNow);

                    await retryRepo.UpdateAsync(freshRestockMsg, CancellationToken.None);

                }

            });

            await Task.WhenAll(taskSale, taskRestock);

        }

        // Assert: Deterministic final stock is exactly 9 (10 - 2 + 1)

        using (var finalScope = _serviceProvider.CreateScope())

        {

            var mediator = finalScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(9m, stock!.QuantityOnHand);

            var orderTxs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Order", orderId));

            Assert.Single(orderTxs);

            Assert.Equal(2m, orderTxs.First().Quantity);

            var refundTxs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", refundId));

            Assert.Single(refundTxs);

            Assert.Equal(1m, refundTxs.First().Quantity);

        }

    }

    // =================================================================================================

    // 7. Missing Correlation: Standalone Restock Quarantined Without Inventing Stock

    // =================================================================================================

    [Fact]

    public async Task Scenario7_MissingCorrelation_StandaloneRestockAttempt_QuarantinesInventoryActionWithoutInventingStock()

    {

        var variantId = await SeedTrackedStockAsync(10m);

        var refundId = Guid.NewGuid();

        // Construct a restock outbox message without OriginalOrderId (simulating missing correlation / legacy refund)

        var payload = new InventoryPostingPayload(

            refundId,

            "REF-STANDALONE-TEST",

            _warehouseId.Value,

            [new InventoryPostingLineItem(variantId.Value, "SKU-TEST", "Product", 2m)],

            IsRestock: true,

            ReferenceType: "Refund",

            OriginalOrderId: null,

            OriginalOrderNumber: null);

        var restockMsg = OutboxMessage.Create(

            OutboxMessageType.InventoryPosting,

            "Refund",

            refundId.ToString(),

            refundId.ToString(),

            JsonSerializer.Serialize(payload),

            $"RESTOCK-{refundId:N}");

        using (var scope = _serviceProvider.CreateScope())

        {

            var handler = scope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            // Handler must throw InvalidOperationException quarantining the standalone restock

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(

                () => handler.HandleAsync(restockMsg, CancellationToken.None));

            Assert.Contains("missing required original sale correlation", ex.Message);

            Assert.Contains("Standalone restock is disabled", ex.Message);

        }

        // Verify stock is STILL 10 (never invented stock without correlation)

        using (var verifyScope = _serviceProvider.CreateScope())

        {

            var mediator = verifyScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(10m, stock!.QuantityOnHand);

            var txs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", refundId));

            Assert.Empty(txs);

        }

    }

    // =================================================================================================

    // 8. Concurrent Duplicate Sales Under Separate DbContexts (Atomic Deduplication)

    // =================================================================================================

    [Fact]

    public async Task Scenario8_ConcurrentDuplicateSales_SeparateDbContexts_AppLockAndConstraintDeduplicationEnsuresExactlyOnceDeduction()

    {

        var variantId = await SeedTrackedStockAsync(10m);

        var (orderId, _) = await CreateAndCompleteOrderAsync(variantId, quantity: 3m);

        using (var scope1 = _serviceProvider.CreateScope())

        using (var scope2 = _serviceProvider.CreateScope())

        {

            var repo1 = scope1.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var repo2 = scope2.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var saleMsg1 = await repo1.GetByIdempotencyKeyAsync($"inv-{orderId}");

            var saleMsg2 = await repo2.GetByIdempotencyKeyAsync($"inv-{orderId}");

            Assert.NotNull(saleMsg1);

            Assert.NotNull(saleMsg2);

            var handler1 = scope1.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            var handler2 = scope2.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            // Execute concurrently across two separate scopes/DbContexts

            var task1 = Task.Run(() => handler1.HandleAsync(saleMsg1!, CancellationToken.None));

            var task2 = Task.Run(() => handler2.HandleAsync(saleMsg2!, CancellationToken.None));

            await Task.WhenAll(task1, task2);

        }

        // Assert: Exactly 3 units deducted (10 - 3 = 7), NOT 4!

        using (var verifyScope = _serviceProvider.CreateScope())

        {

            var mediator = verifyScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(7m, stock!.QuantityOnHand);

            var txs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Order", orderId));

            Assert.Single(txs);

            Assert.Equal(3m, txs.First().Quantity);

        }

    }

    // =================================================================================================

    // 9. Concurrent Duplicate Refunds Under Separate DbContexts (Atomic Deduplication)

    // =================================================================================================

    [Fact]

    public async Task Scenario9_ConcurrentDuplicateRefunds_SeparateDbContexts_AppLockAndConstraintDeduplicationEnsuresExactlyOnceRestock()

    {

        var variantId = await SeedTrackedStockAsync(10m);

        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(variantId, quantity: 4m);

        // Process sale

        var processor = _serviceProvider.GetRequiredService<IOutboxProcessor>();

        await processor.ProcessBatchAsync();

        // Process refund

        Guid refundId;

        using (var refundScope = _serviceProvider.CreateScope())

        {

            var mediator = refundScope.ServiceProvider.GetRequiredService<IMediator>();

            var refundCmd = new ProcessRefundCommand(

                OrderId: orderId,

                Items: [new RefundItemRequest(orderLineId, 2m, InventoryDisposition.Restock)],

                Reason: "Concurrent duplicate refund test",

                SettlementMethod: RefundSettlementMethod.CashPayout,

                IdempotencyKey: $"REF-CONC-DUP-{Guid.NewGuid():N}",

                CashierId: _cashierId.Value,

                CashierName: "Cashier Alice",

                BranchId: _branchId.Value,

                ShiftId: _shiftId,

                TerminalId: _terminalId.Value);

            var refundDto = await mediator.Send(refundCmd);

            refundId = refundDto.RefundId;

        }

        // Dispatch the same restock message concurrently from 2 separate scopes/DbContexts

        using (var scope1 = _serviceProvider.CreateScope())

        using (var scope2 = _serviceProvider.CreateScope())

        {

            var repo1 = scope1.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var repo2 = scope2.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var msg1 = await repo1.GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");

            var msg2 = await repo2.GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");

            Assert.NotNull(msg1);

            Assert.NotNull(msg2);

            var handler1 = scope1.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            var handler2 = scope2.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            var task1 = Task.Run(() => handler1.HandleAsync(msg1!, CancellationToken.None));

            var task2 = Task.Run(() => handler2.HandleAsync(msg2!, CancellationToken.None));

            await Task.WhenAll(task1, task2);

        }

        // Assert: Exactly 2 units restocked (6 + 2 = 8), NOT 10!

        using (var verifyScope = _serviceProvider.CreateScope())

        {

            var mediator = verifyScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(8m, stock!.QuantityOnHand);

            var txs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", refundId));

            Assert.Single(txs);

            Assert.Equal(2m, txs.First().Quantity);

        }

    }

    // =================================================================================================

    // 10. Multiple Partial Refunds Against Same Order Line (Depletion Cap Enforced)

    // =================================================================================================

    [Fact]
    public async Task Scenario10_MultiplePartialRefunds_SameOrderLine_StrictlyEnforcesDepletionCap_QuarantiningExcessRefunds()
    {
        // 10 units on hand; sell 5 units
        var variantId = await SeedTrackedStockAsync(10m);
        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(variantId, quantity: 5m);

        var processor = _serviceProvider.GetRequiredService<IOutboxProcessor>();
        await processor.ProcessBatchAsync(); // Sale posted: stock = 5, total issued = 5

        async Task<Guid> CreateRefundAsync(decimal qty, string idemp)
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var refundCmd = new ProcessRefundCommand(
                OrderId: orderId,
                Items: [new RefundItemRequest(orderLineId, qty, InventoryDisposition.Restock)],
                Reason: "Partial refund sequence",
                SettlementMethod: RefundSettlementMethod.CashPayout,
                IdempotencyKey: idemp,
                CashierId: _cashierId.Value,
                CashierName: "Cashier Alice",
                BranchId: _branchId.Value,
                ShiftId: _shiftId,
                TerminalId: _terminalId.Value);

            var dto = await mediator.Send(refundCmd);
            return dto.RefundId;
        }

        // Partial Refund 1: refund 2 units -> restocks 2 (stock becomes 7)
        var refund1Id = await CreateRefundAsync(2m, "PARTIAL-REF-1");
        using (var s = _serviceProvider.CreateScope())
        {
            var repo = s.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var msg1 = await repo.GetByIdempotencyKeyAsync($"RESTOCK-{refund1Id:N}");
            Assert.NotNull(msg1);

            var h = s.ServiceProvider.GetServices<IOutboxMessageHandler>().Single(x => x.MessageType == OutboxMessageType.InventoryPosting);
            await h.HandleAsync(msg1!, CancellationToken.None);
            msg1!.MarkCompleted(DateTimeOffset.UtcNow);
            await repo.UpdateAsync(msg1, CancellationToken.None);
        }

        // Partial Refund 2: refund 2 units -> restocks 2 (stock becomes 9)
        var refund2Id = await CreateRefundAsync(2m, "PARTIAL-REF-2");
        using (var s = _serviceProvider.CreateScope())
        {
            var repo = s.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var msg2 = await repo.GetByIdempotencyKeyAsync($"RESTOCK-{refund2Id:N}");
            Assert.NotNull(msg2);

            var h = s.ServiceProvider.GetServices<IOutboxMessageHandler>().Single(x => x.MessageType == OutboxMessageType.InventoryPosting);
            await h.HandleAsync(msg2!, CancellationToken.None);
            msg2!.MarkCompleted(DateTimeOffset.UtcNow);
            await repo.UpdateAsync(msg2, CancellationToken.None);
        }

        // Partial Refund 3: refund 1 unit -> restocks 1 (stock becomes 10, all 5 issued units restocked)
        var refund3Id = await CreateRefundAsync(1m, "PARTIAL-REF-3");
        using (var s = _serviceProvider.CreateScope())
        {
            var repo = s.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var msg3 = await repo.GetByIdempotencyKeyAsync($"RESTOCK-{refund3Id:N}");
            Assert.NotNull(msg3);

            var h = s.ServiceProvider.GetServices<IOutboxMessageHandler>().Single(x => x.MessageType == OutboxMessageType.InventoryPosting);
            await h.HandleAsync(msg3!, CancellationToken.None);
            msg3!.MarkCompleted(DateTimeOffset.UtcNow);
            await repo.UpdateAsync(msg3, CancellationToken.None);
        }

        // Now test Capping and Excess Quarantine:
        // Construct an outbox restock payload with 2 units for this order:
        // Total issued = 5, already restocked = 5 -> remaining restockable = 0!
        var excessRefundId = Guid.NewGuid();
        var excessPayload = new InventoryPostingPayload(
            excessRefundId,
            "REF-EXCESS-CAP",
            _warehouseId.Value,
            [new InventoryPostingLineItem(variantId.Value, "SKU-TEST", "Product", 2m)],
            IsRestock: true,
            ReferenceType: "Refund",
            OriginalOrderId: orderId,
            OriginalOrderNumber: "ORD-ORIG");

        var excessMsg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Refund",
            excessRefundId.ToString(),
            excessRefundId.ToString(),
            JsonSerializer.Serialize(excessPayload),
            $"RESTOCK-{excessRefundId:N}");

        using (var s = _serviceProvider.CreateScope())
        {
            var h = s.ServiceProvider.GetServices<IOutboxMessageHandler>().Single(x => x.MessageType == OutboxMessageType.InventoryPosting);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => h.HandleAsync(excessMsg, CancellationToken.None));
            Assert.Contains("quarantined", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("have already been restocked", ex.Message, StringComparison.OrdinalIgnoreCase);

            excessMsg.MarkDeadLetter(ex.Message);
            await s.ServiceProvider.GetRequiredService<IOutboxRepository>().AddAsync(excessMsg, CancellationToken.None);
        }

        // Assert: Stock is strictly 10 (never exceeds original stock), exactly 5 units restocked total across refunds 1, 2, 3
        using (var verifyScope = _serviceProvider.CreateScope())
        {
            var mediator = verifyScope.ServiceProvider.GetRequiredService<IMediator>();
            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));
            Assert.NotNull(stock);
            Assert.Equal(10m, stock!.QuantityOnHand);

            var txs3 = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", refund3Id));
            Assert.Single(txs3);
            Assert.Equal(1m, txs3.First().Quantity);

            var txsExcess = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", excessRefundId));
            Assert.Empty(txsExcess); // Quarantined: 0 restocked
        }
    }

    // =================================================================================================

    [Fact]

    public async Task Scenario11_PostSaleStockCreation_DoesNotFabricateReturnEntitlement_QuarantinesRestock()

    {

        // 1. Untracked product sold (0 units on hand, no stock row)

        var untrackedVariantId = new ProductVariantId(Guid.NewGuid());

        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(untrackedVariantId, quantity: 2m);

        var processor = _serviceProvider.GetRequiredService<IOutboxProcessor>();

        await processor.ProcessBatchAsync(); // Sale processed: untracked, 0 issued

        // 2. Customer performs financial refund

        Guid refundId;

        using (var refundScope = _serviceProvider.CreateScope())

        {

            var mediator = refundScope.ServiceProvider.GetRequiredService<IMediator>();

            var refundCmd = new ProcessRefundCommand(

                OrderId: orderId,

                Items: [new RefundItemRequest(orderLineId, 2m, InventoryDisposition.Restock)],

                Reason: "Post-sale tracking test",

                SettlementMethod: RefundSettlementMethod.CashPayout,

                IdempotencyKey: $"REF-POST-SALE-TRACK-{Guid.NewGuid():N}",

                CashierId: _cashierId.Value,

                CashierName: "Cashier Alice",

                BranchId: _branchId.Value,

                ShiftId: _shiftId,

                TerminalId: _terminalId.Value);

            var dto = await mediator.Send(refundCmd);

            refundId = dto.RefundId;

        }

        // 3. AFTER sale, someone enables tracking by creating a WarehouseStock row with 5 units opening balance

        using (var postSaleScope = _serviceProvider.CreateScope())

        {

            var mediator = postSaleScope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new OpenOrReceiveStockCommand(

                _warehouseId.Value,

                untrackedVariantId.Value,

                5m,

                "Enabling tracking after sale",

                "ManualSetup",

                Guid.NewGuid()));

        }

        // 4. Now process restock outbox message

        using (var restockScope = _serviceProvider.CreateScope())

        {

            var outboxRepo = restockScope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var restockMsg = await outboxRepo.GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");

            Assert.NotNull(restockMsg);

            var handler = restockScope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            // Handler inspects original sale depletion evidence: finds 0 units issued!

            // It MUST throw InvalidOperationException quarantining the restock.

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(

                () => handler.HandleAsync(restockMsg!, CancellationToken.None));

            Assert.Contains("quarantined", ex.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Contains("no sale depletion evidence", ex.Message, StringComparison.OrdinalIgnoreCase);

            restockMsg!.MarkDeadLetter(ex.Message);

            await outboxRepo.UpdateAsync(restockMsg, CancellationToken.None);

        }

        // 5. Assert: Stock remains strictly 5 (did NOT fabricate +2 to 7!)

        using (var verifyScope = _serviceProvider.CreateScope())

        {

            var mediator = verifyScope.ServiceProvider.GetRequiredService<IMediator>();

            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, untrackedVariantId.Value));

            Assert.NotNull(stock);

            Assert.Equal(5m, stock!.QuantityOnHand);

            var refundTxs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", refundId));

            Assert.Empty(refundTxs);

        }

    }

    // =================================================================================================

    // 12. Warehouse Mismatch: Restock Targeted at Different Warehouse Quarantined

    // =================================================================================================

    [Fact]

    public async Task Scenario12_WarehouseMismatch_RefundRestockTargetedAtDifferentWarehouse_QuarantinesAction()

    {

        var variantId = await SeedTrackedStockAsync(10m); // Seeded at _warehouseId

        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(variantId, quantity: 2m);

        var processor = _serviceProvider.GetRequiredService<IOutboxProcessor>();

        await processor.ProcessBatchAsync(); // Sale issued at _warehouseId: stock = 8

        // Seed Warehouse 2 with 10 units of the same variant

        var warehouse2Id = new WarehouseId(Guid.NewGuid());

        using (var seedWh2Scope = _serviceProvider.CreateScope())

        {

            var mediator = seedWh2Scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new OpenOrReceiveStockCommand(

                warehouse2Id.Value,

                variantId.Value,

                10m,

                "Warehouse 2 initial stock",

                "Seed",

                Guid.NewGuid()));

        }

        var refundId = Guid.NewGuid();

        // Construct restock message targeting Warehouse 2 for an order sold at _warehouseId

        var payload = new InventoryPostingPayload(

            refundId,

            "REF-WH-MISMATCH",

            warehouse2Id.Value,

            [new InventoryPostingLineItem(variantId.Value, "SKU-TEST", "Product", 2m)],

            IsRestock: true,

            ReferenceType: "Refund",

            OriginalOrderId: orderId,

            OriginalOrderNumber: "ORD-ORIG");

        var restockMsg = OutboxMessage.Create(

            OutboxMessageType.InventoryPosting,

            "Refund",

            refundId.ToString(),

            refundId.ToString(),

            JsonSerializer.Serialize(payload),

            $"RESTOCK-{refundId:N}");

        using (var scope = _serviceProvider.CreateScope())

        {

            var handler = scope.ServiceProvider.GetServices<IOutboxMessageHandler>()

                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(

                () => handler.HandleAsync(restockMsg, CancellationToken.None));

            Assert.Contains("quarantined", ex.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Contains("no sale depletion evidence at warehouse", ex.Message, StringComparison.OrdinalIgnoreCase);

        }

        // Assert: Warehouse 2 stock is STILL 10 (no cross-warehouse contamination)

        using (var verifyScope = _serviceProvider.CreateScope())

        {

            var mediator = verifyScope.ServiceProvider.GetRequiredService<IMediator>();

            var stockWh2 = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(warehouse2Id.Value, variantId.Value));

            Assert.NotNull(stockWh2);

            Assert.Equal(10m, stockWh2!.QuantityOnHand);

            var stockWh1 = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));

            Assert.NotNull(stockWh1);

            Assert.Equal(8m, stockWh1!.QuantityOnHand);

        }

    }


    // =================================================================================================
    // 13. Multi-Item Message: Failure Midway Retries Without Skipping Unfinished or Repeating Completed
    // =================================================================================================
    [Fact]
    public async Task Scenario13_MultiItemMessage_FailureMidway_RetriesWithoutSkippingUnfinishedOrRepeatingCompleted()
    {
        // Item A and Item C have stock = 5m
        var variantA = await SeedTrackedStockAsync(5m);
        var variantB = await SeedTrackedStockAsync(0m); // Item B has 0 stock, AllowNegativeStock = false
        var variantC = await SeedTrackedStockAsync(5m);

        var orderId = Guid.NewGuid();
        var items = new List<InventoryPostingLineItem>
        {
            new(variantA.Value, "SKU-A", "Product A", 2m),
            new(variantB.Value, "SKU-B", "Product B", 2m),
            new(variantC.Value, "SKU-C", "Product C", 2m)
        };

        var payload = new InventoryPostingPayload(
            orderId,
            "ORD-MULTI-RETRY",
            _warehouseId.Value,
            items);

        var msg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            orderId.ToString(),
            orderId.ToString(),
            JsonSerializer.Serialize(payload),
            $"inv-{orderId:N}");

        // Attempt 1: Item A succeeds, Item B throws InsufficientStockException, Item C not reached
        using (var scope1 = _serviceProvider.CreateScope())
        {
            var handler = scope1.ServiceProvider.GetServices<IOutboxMessageHandler>()
                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            await Assert.ThrowsAnyAsync<Exception>(() => handler.HandleAsync(msg, CancellationToken.None));
        }

        // Verify intermediate state:
        // Item A: issued 2 units (stock 5 -> 3)
        // Item B: stock 0
        // Item C: stock 5
        using (var midScope = _serviceProvider.CreateScope())
        {
            var mediator = midScope.ServiceProvider.GetRequiredService<IMediator>();
            var stockA = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantA.Value));
            var stockB = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantB.Value));
            var stockC = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantC.Value));
            Assert.Equal(3m, stockA!.QuantityOnHand);
            Assert.Equal(0m, stockB!.QuantityOnHand);
            Assert.Equal(5m, stockC!.QuantityOnHand);

            var txsA = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Order", orderId));
            Assert.Single(txsA); // Exactly 1 movement for Item A
            Assert.Equal(variantA.Value, txsA.First().ProductVariantId);
        }

        // Replenish stock for Item B so retry can succeed
        using (var replenishScope = _serviceProvider.CreateScope())
        {
            var mediator = replenishScope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new OpenOrReceiveStockCommand(
                _warehouseId.Value,
                variantB.Value,
                5m,
                "Replenishment for retry",
                "PO",
                Guid.NewGuid()));
        }

        // Attempt 2 (Retry): Item A must NOT be repeated, Item B and Item C must be processed
        using (var scope2 = _serviceProvider.CreateScope())
        {
            var handler = scope2.ServiceProvider.GetServices<IOutboxMessageHandler>()
                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            await handler.HandleAsync(msg, CancellationToken.None);
        }

        // Assert final state:
        // Item A: stock remains 3 (NOT double-deducted to 1!)
        // Item B: stock drops from 5 to 3
        // Item C: stock drops from 5 to 3
        using (var finalScope = _serviceProvider.CreateScope())
        {
            var mediator = finalScope.ServiceProvider.GetRequiredService<IMediator>();
            var stockA = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantA.Value));
            var stockB = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantB.Value));
            var stockC = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantC.Value));
            Assert.Equal(3m, stockA!.QuantityOnHand);
            Assert.Equal(3m, stockB!.QuantityOnHand);
            Assert.Equal(3m, stockC!.QuantityOnHand);

            var allTxs = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Order", orderId));
            Assert.Equal(3, allTxs.Count);
            Assert.Contains(allTxs, t => t.ProductVariantId == variantA.Value && t.Quantity == 2m);
            Assert.Contains(allTxs, t => t.ProductVariantId == variantB.Value && t.Quantity == 2m);
            Assert.Contains(allTxs, t => t.ProductVariantId == variantC.Value && t.Quantity == 2m);
        }
    }

    // =================================================================================================
    // 14. Concurrent Distinct Refunds Cannot Together Exceed Original Issued Quantity
    // =================================================================================================
    [Fact]
    public async Task Scenario14_ConcurrentDistinctRefunds_CannotTogetherExceedOriginalIssuedQuantity()
    {
        // 10 units initial stock; order placed for 2 units (stock drops to 8, total issued = 2)
        var variantId = await SeedTrackedStockAsync(10m);
        var (orderId, orderLineId) = await CreateAndCompleteOrderAsync(variantId, quantity: 2m);

        var processor = _serviceProvider.GetRequiredService<IOutboxProcessor>();
        await processor.ProcessBatchAsync(); // Sale posted: stock = 8, total issued = 2

        // Create two DISTINCT refunds, each for 2 units with Restock disposition
        async Task<(Guid RefundId, OutboxMessage RestockMsg)> CreateRefundAsync(string idemp)
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var refundCmd = new ProcessRefundCommand(
                OrderId: orderId,
                Items: [new RefundItemRequest(orderLineId, 2m, InventoryDisposition.Restock)],
                Reason: "Concurrent refund race",
                SettlementMethod: RefundSettlementMethod.CashPayout,
                IdempotencyKey: idemp,
                CashierId: _cashierId.Value,
                CashierName: "Cashier Alice",
                BranchId: _branchId.Value,
                ShiftId: _shiftId,
                TerminalId: _terminalId.Value);

            var dto = await mediator.Send(refundCmd);
            var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var msg = await repo.GetByIdempotencyKeyAsync($"RESTOCK-{dto.RefundId:N}");
            return (dto.RefundId, msg!);
        }

        var (refund1Id, msg1) = await CreateRefundAsync("RACE-REF-1");

        // Refund 2 is a separate refund message for this order, requesting 2 units
        Refund refund2Entity;
        using (var refundScope = _serviceProvider.CreateScope())
        {
            var db = refundScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            refund2Entity = Refund.Create(
                RefundNumber.Generate(DateTimeOffset.UtcNow),
                new OrderId(orderId),
                _branchId,
                _warehouseId,
                _cashierId.Value,
                "Cashier Alice",
                "Concurrent refund race 2",
                "RACE-REF-2",
                RefundSettlementMethod.CashPayout,
                shiftId: new ShiftId(_shiftId),
                terminalId: _terminalId);
            await db.Refunds.AddAsync(refund2Entity);
            await db.SaveChangesAsync();
        }

        var refund2Id = refund2Entity.Id.Value;
        var payload2 = new InventoryPostingPayload(
            refund2Id,
            "REF-RACE-2",
            _warehouseId.Value,
            [new InventoryPostingLineItem(variantId.Value, "SKU-TEST", "Product", 2m)],
            IsRestock: true,
            ReferenceType: "Refund",
            OriginalOrderId: orderId,
            OriginalOrderNumber: "ORD-ORIG");
        var msg2 = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Refund",
            refund2Id.ToString(),
            refund2Id.ToString(),
            JsonSerializer.Serialize(payload2),
            $"RESTOCK-{refund2Id:N}");

        // Now run both restock handlers CONCURRENTLY across 2 separate scopes/DbContexts
        Exception? ex1 = null;
        Exception? ex2 = null;
        using (var scope1 = _serviceProvider.CreateScope())
        using (var scope2 = _serviceProvider.CreateScope())
        {
            var handler1 = scope1.ServiceProvider.GetServices<IOutboxMessageHandler>()
                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);
            var handler2 = scope2.ServiceProvider.GetServices<IOutboxMessageHandler>()
                .Single(h => h.MessageType == OutboxMessageType.InventoryPosting);

            var task1 = Task.Run(async () =>
            {
                try
                {
                    await handler1.HandleAsync(msg1, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    ex1 = ex;
                }
            });

            var task2 = Task.Run(async () =>
            {
                try
                {
                    await handler2.HandleAsync(msg2, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    ex2 = ex;
                }
            });

            await Task.WhenAll(task1, task2);
        }

        // Assert: Exactly ONE succeeded and exactly ONE was quarantined due to atomic depletion cap!
        var loserEx = ex1 ?? ex2;
        Assert.NotNull(loserEx);
        Assert.True(ex1 == null || ex2 == null, "Both concurrent refunds cannot succeed when together they exceed depletion.");
        Assert.IsType<InvalidOperationException>(loserEx);
        Assert.Contains("quarantined", loserEx.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("have already been restocked", loserEx.Message, StringComparison.OrdinalIgnoreCase);

        // Assert: Net stock is STRICTLY 10 (8 + 2), NOT 12!
        using (var verifyScope = _serviceProvider.CreateScope())
        {
            var mediator = verifyScope.ServiceProvider.GetRequiredService<IMediator>();
            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(_warehouseId.Value, variantId.Value));
            Assert.NotNull(stock);
            Assert.Equal(10m, stock!.QuantityOnHand);

            // Exactly 1 receipt transaction across both refunds (either refund 1 or refund 2 won the race)
            var txs1 = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", refund1Id));
            var txs2 = await mediator.Send(new ListInventoryTransactionsByReferenceQuery("Refund", refund2Id));
            if (ex1 != null)
            {
                // Refund 2 won, Refund 1 was quarantined
                Assert.Single(txs2);
                Assert.Equal(2m, txs2.First().Quantity);
                Assert.Empty(txs1);
            }
            else
            {
                // Refund 1 won, Refund 2 was quarantined
                Assert.Single(txs1);
                Assert.Equal(2m, txs1.First().Quantity);
                Assert.Empty(txs2);
            }
        }
    }
}
