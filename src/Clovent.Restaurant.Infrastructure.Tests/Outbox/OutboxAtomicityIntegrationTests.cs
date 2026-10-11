using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Outbox;

public sealed class OutboxAtomicityIntegrationTests : SqliteTestBase
{
    [Fact]
    public async Task A_OrderAndOutboxMessages_CommitAtomicallyInSameTransaction()
    {
        // Arrange
        await using var context = CreateContext();
        var orderRepo = new OrderRepository(context);
        var outboxRepo = new OutboxRepository(context);

        var order = Order.Create(OrderType.TakeAway, new WarehouseId(Guid.NewGuid()), null, OrderNumber.Generate(DateTimeOffset.UtcNow));
        var outboxMsg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            order.Id.Value.ToString(),
            order.Id.Value.ToString(),
            "{\"OrderId\":\"" + order.Id.Value + "\"}",
            $"inv-{order.Id.Value}");

        // Act - Commit in single unit of work
        await using (var tx = await context.Database.BeginTransactionAsync())
        {
            await orderRepo.AddAsync(order);
            await outboxRepo.AddAsync(outboxMsg);
            await context.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // Assert - Both order and outbox message are durably persisted
        await using var verifyContext = CreateContext();
        var savedOrder = await verifyContext.Orders.FindAsync(order.Id);
        var savedOutbox = await verifyContext.OutboxMessages.FindAsync(outboxMsg.Id);

        Assert.NotNull(savedOrder);
        Assert.NotNull(savedOutbox);
        Assert.Equal(OutboxMessageStatus.Pending, savedOutbox.Status);
    }

    [Fact]
    public async Task B_OrderAndOutboxMessages_RollbackAtomically_NoOutboxMessagesSurvive()
    {
        // Arrange
        await using var context = CreateContext();
        var orderRepo = new OrderRepository(context);
        var outboxRepo = new OutboxRepository(context);

        var order = Order.Create(OrderType.TakeAway, new WarehouseId(Guid.NewGuid()), null, OrderNumber.Generate(DateTimeOffset.UtcNow));
        var outboxMsg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            order.Id.Value.ToString(),
            order.Id.Value.ToString(),
            "{}",
            $"inv-{order.Id.Value}");

        // Act - Begin transaction, stage work, then rollback
        await using (var tx = await context.Database.BeginTransactionAsync())
        {
            await orderRepo.AddAsync(order);
            await outboxRepo.AddAsync(outboxMsg);
            await context.SaveChangesAsync();
            await tx.RollbackAsync();
        }

        // Assert - Neither entity survived in DB
        await using var verifyContext = CreateContext();
        var savedOrder = await verifyContext.Orders.FindAsync(order.Id);
        var savedOutbox = await verifyContext.OutboxMessages.FindAsync(outboxMsg.Id);

        Assert.Null(savedOrder);
        Assert.Null(savedOutbox);
    }

    [Fact]
    public async Task C_OutboxEnqueueFailure_AbortsFinancialTransaction()
    {
        // Arrange
        await using var context = CreateContext();
        var order = Order.Create(OrderType.TakeAway, new WarehouseId(Guid.NewGuid()), null, OrderNumber.Generate(DateTimeOffset.UtcNow));
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Seed an outbox message with idempotency key
        var existingMsg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting, "Order", order.Id.Value.ToString(), order.Id.Value.ToString(), "{}", "unique-idem-key");
        context.OutboxMessages.Add(existingMsg);
        await context.SaveChangesAsync();

        // Act: Attempt to insert duplicate outbox message in a new transaction
        var duplicateMsg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting, "Order", order.Id.Value.ToString(), order.Id.Value.ToString(), "{}", "unique-idem-key");

        bool rolledBack = false;
        try
        {
            await using var tx = await context.Database.BeginTransactionAsync();
            order.Complete();
            context.Orders.Update(order);
            context.OutboxMessages.Add(duplicateMsg);
            await context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException)
        {
            rolledBack = true;
        }

        // Assert: Unique constraint failed and order status was not committed as Completed
        Assert.True(rolledBack);

        await using var verifyContext = CreateContext();
        var freshOrder = await verifyContext.Orders.FindAsync(order.Id);
        Assert.NotNull(freshOrder);
        Assert.NotEqual(OrderStatus.Completed, freshOrder.Status);
    }

    [Fact]
    public async Task D_ProcessDiesImmediatelyAfterDbCommit_CommittedOutboxWorkIsRecovered()
    {
        // Arrange: Commit financial transaction with outbox work
        var orderId = Guid.NewGuid();
        var outboxMsgId = Guid.NewGuid();
        await using (var context = CreateContext())
        {
            var msg = OutboxMessage.Create(
                OutboxMessageType.QuickBooksSync, "Order", orderId.ToString(), orderId.ToString(), "{\"Amount\":100}", $"qb-{orderId}");
            context.OutboxMessages.Add(msg);
            await context.SaveChangesAsync();
            // Process terminates here: context is disposed
        }

        // Act: New application instance boots up and polls outbox
        await using (var newProcessContext = CreateContext())
        {
            var outboxRepo = new OutboxRepository(newProcessContext);
            var pendingWork = await outboxRepo.ClaimMessagesAsync(batchSize: 10);

            // Assert: Outbox work is recovered and available for background processing
            Assert.Single(pendingWork);
            Assert.Equal(orderId.ToString(), pendingWork[0].AggregateId);
            Assert.Equal(OutboxMessageStatus.Processing, pendingWork[0].Status);
        }
    }

    [Fact]
    public async Task E_TwoWorkersAttemptSameMessage_OptimisticConcurrencyPreventsDuplicateExecution()
    {
        // Arrange
        var msgId = Guid.NewGuid();
        await using (var initContext = CreateContext())
        {
            var msg = OutboxMessage.Create(
                OutboxMessageType.ReceiptPrint, "Order", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "{}", "print-1");
            initContext.OutboxMessages.Add(msg);
            await initContext.SaveChangesAsync();
        }

        // Worker 1 and Worker 2 load the same message concurrently
        await using var worker1Context = CreateContext();
        await using var worker2Context = CreateContext();

        var msg1 = await worker1Context.OutboxMessages.FirstAsync();
        var msg2 = await worker2Context.OutboxMessages.FirstAsync();

        // Worker 1 acquires and transitions to Processing
        msg1.ClaimForProcessing();
        await worker1Context.SaveChangesAsync();

        // Worker 2 attempts to acquire the same message
        msg2.ClaimForProcessing();

        // Assert: Worker 2 fails with concurrency exception
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await worker2Context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task F_Payments_UniqueIdempotencyKey_ConstraintEnforced()
    {
        await using var context = CreateContext();
        var orderId = new OrderId(Guid.NewGuid());
        var methodId = new PaymentMethodId(Guid.NewGuid());

        var payment1 = Payment.Create(orderId, methodId, 100m, null, "unique-payment-idem-key");
        var payment2 = Payment.Create(orderId, methodId, 100m, null, "unique-payment-idem-key");

        context.Payments.Add(payment1);
        await context.SaveChangesAsync();

        context.Payments.Add(payment2);
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task G_OutboxProcessor_ConcurrentBatchWithQuickBooksSyncMaps_CompletesWithoutDbContextThreadCollision()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"cbos-outbox-conc-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        try
        {
            var services = new ServiceCollection();
            services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>));
            services.AddDbContext<RestaurantDbContext>(opts => opts.UseSqlite(connectionString));
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<Clovent.Restaurant.QuickBooks.IQuickBooksSyncMapRepository, QuickBooksSyncMapRepository>();
            services.AddSingleton<Clovent.Platform.CircuitBreakers.ICircuitBreakerRegistry, Clovent.Platform.CircuitBreakers.CircuitBreakerRegistry>();
            services.AddSingleton<Clovent.Restaurant.Application.QuickBooks.IQuickBooksGateway>(new Clovent.Restaurant.Application.QuickBooks.DefaultQuickBooksGateway());
            services.AddScoped<Clovent.Restaurant.Application.Outbox.IOutboxMessageHandler, Clovent.Restaurant.Application.Outbox.Handlers.QuickBooksSyncOutboxHandler>();

            await using var provider = services.BuildServiceProvider();

            using (var initScope = provider.CreateScope())
            {
                var initDb = initScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
                await initDb.Database.EnsureCreatedAsync();

                for (int i = 0; i < 6; i++)
                {
                    var orderId = Guid.NewGuid();
                    var payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        OrderId = orderId,
                        OrderNumber = $"ORD-QB-{i + 1:000}",
                        TotalAmount = 150m + (i * 25m),
                        TaxAmount = 15m,
                        DiscountAmount = 0m,
                        CompletedAtUtc = DateTimeOffset.UtcNow
                    });

                    var msg = OutboxMessage.Create(
                        OutboxMessageType.QuickBooksSync,
                        "Order",
                        orderId.ToString(),
                        orderId.ToString(),
                        payload,
                        $"qb-conc-{orderId}");
                    initDb.OutboxMessages.Add(msg);
                }

                await initDb.SaveChangesAsync();
            }

            using var processor = new Clovent.Restaurant.Infrastructure.Outbox.OutboxProcessor(
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Clovent.Restaurant.Infrastructure.Outbox.OutboxProcessor>>());

            var processed = await processor.ProcessBatchAsync();
            Assert.Equal(6, processed);

            using var verifyScope = provider.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            var allMessages = await verifyDb.OutboxMessages.ToListAsync();
            var allSyncMaps = await verifyDb.QuickBooksSyncMaps.ToListAsync();

            Assert.Equal(6, allMessages.Count);
            Assert.All(allMessages, m =>
            {
                Assert.Equal(OutboxMessageStatus.Completed, m.Status);
                Assert.Null(m.LastError);
            });

            Assert.Equal(6, allSyncMaps.Count);
            Assert.All(allSyncMaps, map =>
            {
                Assert.Equal(Clovent.Restaurant.QuickBooks.QuickBooksSyncStatus.Synchronized, map.Status);
                Assert.NotNull(map.QuickBooksTxnId);
            });
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (System.IO.File.Exists(dbPath))
            {
                try { System.IO.File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task H_OutboxProcessor_RetainsRetryIdempotencyAndGracefulShutdownUnderScopeIsolation()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"cbos-outbox-retry-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        try
        {
            var gateway = new Clovent.Restaurant.Application.QuickBooks.DefaultQuickBooksGateway();
            var services = new ServiceCollection();
            services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>));
            services.AddDbContext<RestaurantDbContext>(opts => opts.UseSqlite(connectionString));
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<Clovent.Restaurant.QuickBooks.IQuickBooksSyncMapRepository, QuickBooksSyncMapRepository>();
            services.AddSingleton<Clovent.Platform.CircuitBreakers.ICircuitBreakerRegistry, Clovent.Platform.CircuitBreakers.CircuitBreakerRegistry>();
            services.AddSingleton<Clovent.Restaurant.Application.QuickBooks.IQuickBooksGateway>(gateway);
            services.AddScoped<Clovent.Restaurant.Application.Outbox.IOutboxMessageHandler, Clovent.Restaurant.Application.Outbox.Handlers.QuickBooksSyncOutboxHandler>();

            await using var provider = services.BuildServiceProvider();
            var orderId = Guid.NewGuid();
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                OrderId = orderId,
                OrderNumber = "ORD-QB-RETRY-001",
                TotalAmount = 250m,
                TaxAmount = 25m,
                DiscountAmount = 0m,
                CompletedAtUtc = DateTimeOffset.UtcNow
            });

            using (var initScope = provider.CreateScope())
            {
                var initDb = initScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
                await initDb.Database.EnsureCreatedAsync();

                initDb.OutboxMessages.Add(OutboxMessage.Create(
                    OutboxMessageType.QuickBooksSync,
                    "Order",
                    orderId.ToString(),
                    orderId.ToString(),
                    payload,
                    $"qb-retry-1-{orderId}"));
                await initDb.SaveChangesAsync();
            }

            using var processor = new Clovent.Restaurant.Infrastructure.Outbox.OutboxProcessor(
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Clovent.Restaurant.Infrastructure.Outbox.OutboxProcessor>>());

            // 1. Simulate QuickBooks outage -> message must NOT be marked Completed; must transition to RetryScheduled
            gateway.SetSimulatedOutage(true);
            var firstBatch = await processor.ProcessBatchAsync();
            Assert.Equal(1, firstBatch);

            using (var checkScope = provider.CreateScope())
            {
                var checkDb = checkScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
                var failedMsg = await checkDb.OutboxMessages.SingleAsync();
                Assert.Equal(OutboxMessageStatus.RetryScheduled, failedMsg.Status);
                Assert.Equal(1, failedMsg.AttemptCount);
                Assert.NotNull(failedMsg.LastError);

                var failedMap = await checkDb.QuickBooksSyncMaps.SingleAsync();
                Assert.Equal(Clovent.Restaurant.QuickBooks.QuickBooksSyncStatus.Failed, failedMap.Status);

                // Make message immediately eligible for retry and enqueue a second message for the same OrderId to test idempotency
                failedMsg.RetryNow();
                checkDb.OutboxMessages.Add(OutboxMessage.Create(
                    OutboxMessageType.QuickBooksSync,
                    "Order",
                    orderId.ToString(),
                    orderId.ToString(),
                    payload,
                    $"qb-retry-2-{orderId}"));
                await checkDb.SaveChangesAsync();
            }

            // 2. Restore gateway connectivity and process retry + duplicate concurrently
            gateway.SetSimulatedOutage(false);
            var secondBatch = await processor.ProcessBatchAsync();
            Assert.Equal(2, secondBatch);

            using (var verifyScope = provider.CreateScope())
            {
                var verifyDb = verifyScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
                var allMessages = await verifyDb.OutboxMessages.ToListAsync();
                var allMaps = await verifyDb.QuickBooksSyncMaps.ToListAsync();

                Assert.Equal(2, allMessages.Count);
                Assert.All(allMessages, m => Assert.Equal(OutboxMessageStatus.Completed, m.Status));
                // Idempotent effect: only one QuickBooksSyncMap record exists for the order and is Synchronized
                Assert.Single(allMaps);
                Assert.Equal(Clovent.Restaurant.QuickBooks.QuickBooksSyncStatus.Synchronized, allMaps[0].Status);
            }

            // 3. Verify graceful background start and stop lifecycle
            await processor.StartAsync(CancellationToken.None);
            Assert.True(processor.IsRunning);
            await processor.StopAsync(CancellationToken.None);
            Assert.False(processor.IsRunning);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (System.IO.File.Exists(dbPath))
            {
                try { System.IO.File.Delete(dbPath); } catch { }
            }
        }
    }
}
