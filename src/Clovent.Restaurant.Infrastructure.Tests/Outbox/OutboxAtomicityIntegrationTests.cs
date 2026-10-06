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
}
