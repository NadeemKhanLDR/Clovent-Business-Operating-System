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

            // Handler should execute cleanly without throwing, but skip restock because totalIssuedForOrder is 0
            await handler.HandleAsync(restockMsg!, CancellationToken.None);
            restockMsg!.MarkCompleted(DateTimeOffset.UtcNow);
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
}
