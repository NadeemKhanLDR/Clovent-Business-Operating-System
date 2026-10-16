using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Infrastructure.Outbox;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Outbox;

/// <summary>
/// Deterministic SQL Server regression tests for Outbox claim concurrency and ownership.
/// Validates that when multiple workers compete for pending messages, only the worker
/// that successfully persists its claim dispatches and processes the message.
/// </summary>
public sealed class OutboxClaimConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly string _databaseName;
    private readonly string _connectionString;

    public OutboxClaimConcurrencyIntegrationTests()
    {
        _databaseName = $"CBOS_OutboxClaimConcurrency_{Guid.NewGuid():N}";
        _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30;";
    }

    public async Task InitializeAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();
        }
        catch
        {
            // Suppress cleanup errors
        }
    }

    private RestaurantDbContext CreateDbContext(ISaveChangesInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(_connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Restaurant"))
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        if (interceptor != null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new RestaurantDbContext(builder.Options);
    }

    [Fact]
    public async Task CompetingWorkers_WhenClaimCollides_LosingWorkerMustNotClaimOrReturnMessage()
    {
        // Arrange: seed 1 pending outbox message
        var messageId = Guid.NewGuid().ToString();
        var pendingMessage = OutboxMessage.Create(
            messageType: "TestType",
            aggregateType: "Order",
            aggregateId: messageId,
            correlationId: Guid.NewGuid().ToString(),
            payload: "{\"test\":true}",
            priority: 1);

        await using (var seedDb = CreateDbContext())
        {
            await seedDb.OutboxMessages.AddAsync(pendingMessage);
            await seedDb.SaveChangesAsync();
        }

        var workerBReadyToSave = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workerAFinishedSaving = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var interceptorB = new CallbackSavingChangesInterceptor(async () =>
        {
            workerBReadyToSave.TrySetResult(true);
            await workerAFinishedSaving.Task;
        });

        await using var dbA = CreateDbContext();
        await using var dbB = CreateDbContext(interceptorB);

        var repoA = new OutboxRepository(dbA);
        var repoB = new OutboxRepository(dbB);

        // Act: Worker B starts claiming and pauses in SavingChanges
        var workerBTask = Task.Run(async () => await repoB.ClaimMessagesAsync(10));

        await workerBReadyToSave.Task;

        // Worker A claims the message and commits first
        var workerAClaimed = await repoA.ClaimMessagesAsync(10);
        Assert.Single(workerAClaimed);
        Assert.Equal(pendingMessage.Id, workerAClaimed[0].Id);

        // Signal Worker B to proceed into SaveChanges, causing a concurrency collision
        workerAFinishedSaving.TrySetResult(true);
        var workerBClaimed = await workerBTask;

        // Assert:
        // Worker A successfully claimed the message
        Assert.Single(workerAClaimed);

        // Worker B's database update was rejected by SQL Server due to concurrency token mismatch.
        // Therefore, Worker B must NOT claim or return the message.
        Assert.Empty(workerBClaimed);

        // Verify in database: message is in Processing status with attempt count = 1
        await using var verifyDb = CreateDbContext();
        var messageInDb = await verifyDb.OutboxMessages.FindAsync(pendingMessage.Id);
        Assert.NotNull(messageInDb);
        Assert.Equal(OutboxMessageStatus.Processing, messageInDb.Status);
        Assert.Equal(1, messageInDb.AttemptCount);
    }

    [Fact]
    public async Task CompetingWorkers_InProcessBatch_LosingWorkerMustNotDispatchHandlers()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var pendingMessage = OutboxMessage.Create(
            messageType: "ExclusiveTestType",
            aggregateType: "Order",
            aggregateId: messageId,
            correlationId: Guid.NewGuid().ToString(),
            payload: "{\"key\":\"val\"}");

        await using (var seedDb = CreateDbContext())
        {
            await seedDb.OutboxMessages.AddAsync(pendingMessage);
            await seedDb.SaveChangesAsync();
        }

        var testHandler = new SpyOutboxMessageHandler("ExclusiveTestType");

        var workerBReadyToSave = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workerAFinishedSaving = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var interceptorB = new CallbackSavingChangesInterceptor(async () =>
        {
            workerBReadyToSave.TrySetResult(true);
            await workerAFinishedSaving.Task;
        });

        // Setup DI for Worker A
        var servicesA = new ServiceCollection();
        servicesA.AddLogging();
        servicesA.AddScoped<IOutboxRepository, OutboxRepository>();
        servicesA.AddScoped(_ => CreateDbContext());
        servicesA.AddSingleton<IOutboxMessageHandler>(testHandler);
        var spA = servicesA.BuildServiceProvider();

        // Setup DI for Worker B (with interceptor)
        var servicesB = new ServiceCollection();
        servicesB.AddLogging();
        servicesB.AddScoped<IOutboxRepository, OutboxRepository>();
        servicesB.AddScoped(_ => CreateDbContext(interceptorB));
        servicesB.AddSingleton<IOutboxMessageHandler>(testHandler);
        var spB = servicesB.BuildServiceProvider();

        var processorA = new OutboxProcessor(spA.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxProcessor>.Instance);
        var processorB = new OutboxProcessor(spB.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxProcessor>.Instance);

        // Act: Worker B starts processing batch and pauses right before SaveChanges in ClaimMessagesAsync
        var workerBTask = Task.Run(async () => await processorB.ProcessBatchAsync());

        await workerBReadyToSave.Task;

        // Worker A processes the batch and claims the message
        var countA = await processorA.ProcessBatchAsync();
        Assert.Equal(1, countA);

        // Allow Worker B to resume and encounter concurrency violation
        workerAFinishedSaving.TrySetResult(true);
        var countB = await workerBTask;

        // Assert:
        // Worker A processed 1 message, Worker B processed 0 messages
        Assert.Equal(1, countA);
        Assert.Equal(0, countB);

        // Handler must be dispatched EXACTLY ONCE across competing workers
        Assert.Equal(1, testHandler.ExecutionCount);
    }

    [Fact]
    public async Task PartialBatchCollision_WorkerClaimsNonConflictingMessages_AndLeavesUnrelatedEntitiesUntouched()
    {
        // Arrange: seed 2 pending outbox messages
        var msg1 = OutboxMessage.Create("TypeA", "Order", "1", "c1", "{}", priority: 10);
        var msg2 = OutboxMessage.Create("TypeB", "Order", "2", "c2", "{}", priority: 5);

        await using (var seedDb = CreateDbContext())
        {
            await seedDb.OutboxMessages.AddRangeAsync(msg1, msg2);
            await seedDb.SaveChangesAsync();
        }

        var workerBReadyToSave = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workerAFinishedSaving = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var interceptorB = new CallbackSavingChangesInterceptor(async () =>
        {
            if (!workerBReadyToSave.Task.IsCompleted)
            {
                workerBReadyToSave.TrySetResult(true);
                await workerAFinishedSaving.Task;
            }
        });

        await using var dbA = CreateDbContext();
        await using var dbB = CreateDbContext(interceptorB);

        // In dbB, also track an unrelated entity to verify that handling concurrency collision
        // does not blanket-clear unrelated tracked entities in the same DbContext
        var unrelatedLog = ActivityLogEntry.Record("UnrelatedEvent", "Unrelated description", "TestUser", "TestMachine");
        await dbB.ActivityLogEntries.AddAsync(unrelatedLog);

        var repoA = new OutboxRepository(dbA);
        var repoB = new OutboxRepository(dbB);

        // Act: Worker B attempts to claim both messages (batchSize = 10)
        var workerBTask = Task.Run(async () => await repoB.ClaimMessagesAsync(10));

        await workerBReadyToSave.Task;

        // Worker A claims ONLY msg1 (batchSize = 1)
        var workerAClaimed = await repoA.ClaimMessagesAsync(1);
        Assert.Single(workerAClaimed);
        Assert.Equal(msg1.Id, workerAClaimed[0].Id);

        // Worker B resumes: msg1 collides, but msg2 is free
        workerAFinishedSaving.TrySetResult(true);
        var workerBClaimed = await workerBTask;

        // Assert:
        // Worker A claimed msg1
        Assert.Single(workerAClaimed);
        Assert.Equal(msg1.Id, workerAClaimed[0].Id);

        // Worker B should claim msg2 (non-conflicting) and drop msg1 (conflicting)
        Assert.Single(workerBClaimed);
        Assert.Equal(msg2.Id, workerBClaimed[0].Id);

        // Verify the unrelated entity was NOT detached or lost from dbB:
        // It was successfully committed along with the non-conflicting message msg2
        var entry = dbB.Entry(unrelatedLog);
        Assert.Equal(EntityState.Unchanged, entry.State);

        await using var verifyDb = CreateDbContext();
        var savedLog = await verifyDb.ActivityLogEntries.FindAsync(unrelatedLog.Id);
        Assert.NotNull(savedLog);
    }

    [Fact]
    public async Task RetryScheduledAndStaleMessages_RemainClaimableAndRecoverable()
    {
        // Arrange: 1 RetryScheduled message (due now) and 1 stale Processing message (started 10 mins ago)
        var retryMsg = OutboxMessage.Create("RetryType", "Order", "3", "c3", "{}");
        retryMsg.ScheduleRetry(DateTimeOffset.UtcNow.AddSeconds(-5), "Previous transient failure");

        var staleMsg = OutboxMessage.Create("StaleType", "Order", "4", "c4", "{}");
        staleMsg.MarkProcessing(DateTimeOffset.UtcNow.AddMinutes(-10));

        await using (var seedDb = CreateDbContext())
        {
            await seedDb.OutboxMessages.AddRangeAsync(retryMsg, staleMsg);
            await seedDb.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped(_ => CreateDbContext());
        var sp = services.BuildServiceProvider();

        var processor = new OutboxProcessor(sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxProcessor>.Instance);

        // Act 1: Recover stale messages
        var recoveredCount = await processor.RecoverStaleProcessingMessagesAsync(TimeSpan.FromMinutes(2));
        Assert.Equal(1, recoveredCount);

        // Act 2: Claim messages
        await using var db = CreateDbContext();
        var repo = new OutboxRepository(db);
        var claimed = await repo.ClaimMessagesAsync(10);

        // Assert: both the due retry and the recovered stale message must be claimed
        Assert.Equal(2, claimed.Count);
        Assert.Contains(claimed, m => m.Id == retryMsg.Id);
        Assert.Contains(claimed, m => m.Id == staleMsg.Id);
    }

    [Fact]
    public async Task UnrelatedTrackedEntity_ConcurrencyConflict_PropagatesExceptionWithoutSwallowing()
    {
        // Arrange: seed 1 pending message (so ClaimMessagesAsync attempts a claim)
        // and 1 unrelated message in Completed status (never eligible for claim)
        var pendingMsg = OutboxMessage.Create("PendingType", "Order", "p1", "c-p1", "{}");
        var unrelatedMsg = OutboxMessage.Create("UnrelatedType", "Order", "u1", "c-u1", "{}");
        unrelatedMsg.MarkProcessing(DateTimeOffset.UtcNow.AddMinutes(-5));
        unrelatedMsg.MarkCompleted(DateTimeOffset.UtcNow.AddMinutes(-4));

        await using (var seedDb = CreateDbContext())
        {
            await seedDb.OutboxMessages.AddRangeAsync(pendingMsg, unrelatedMsg);
            await seedDb.SaveChangesAsync();
        }

        await using var dbB = CreateDbContext();
        var unrelatedInB = await dbB.OutboxMessages.FindAsync(unrelatedMsg.Id);
        Assert.NotNull(unrelatedInB);
        unrelatedInB.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(5), "Simulated external change");

        // Concurrent context modifies and saves the unrelated entity first, incrementing Version in SQL Server
        await using (var dbOther = CreateDbContext())
        {
            var unrelatedInOther = await dbOther.OutboxMessages.FindAsync(unrelatedMsg.Id);
            Assert.NotNull(unrelatedInOther);
            unrelatedInOther.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(10), "Other context change");
            await dbOther.SaveChangesAsync();
        }

        var repoB = new OutboxRepository(dbB);

        // Act & Assert:
        // When repoB attempts to claim pending messages, SaveChangesAsync encounters a concurrency violation
        // on the unrelated tracked entity. ClaimMessagesAsync must propagate the exception and NOT detach it.
        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repoB.ClaimMessagesAsync(10));
        Assert.Contains(ex.Entries, e => ReferenceEquals(e.Entity, unrelatedInB));

        // The unrelated entity must NOT have been detached by ClaimMessagesAsync
        var entryInB = dbB.Entry(unrelatedInB);
        Assert.NotEqual(EntityState.Detached, entryInB.State);
    }

    [Fact]
    public async Task MixedEntries_ConcurrencyConflict_PropagatesExceptionWithoutSwallowing()
    {
        // Arrange: seed 1 pending message and 1 unrelated message
        var pendingMsg = OutboxMessage.Create("PendingType", "Order", "p2", "c-p2", "{}");
        var unrelatedMsg = OutboxMessage.Create("UnrelatedType", "Order", "u2", "c-u2", "{}");
        unrelatedMsg.MarkProcessing(DateTimeOffset.UtcNow.AddMinutes(-5));
        unrelatedMsg.MarkCompleted(DateTimeOffset.UtcNow.AddMinutes(-4));

        await using (var seedDb = CreateDbContext())
        {
            await seedDb.OutboxMessages.AddRangeAsync(pendingMsg, unrelatedMsg);
            await seedDb.SaveChangesAsync();
        }

        var interceptor = new ThrowingInterceptor(eventData =>
        {
            var ctx = eventData.Context!;
            var entryClaimed = GetUpdateEntry(ctx.Entry(ctx.Set<OutboxMessage>().Local.First(m => m.Id == pendingMsg.Id)));
            var entryUnrelated = GetUpdateEntry(ctx.Entry(unrelatedMsg));

            return new DbUpdateConcurrencyException(
                "Mixed concurrency violation",
                null,
                new List<Microsoft.EntityFrameworkCore.Update.IUpdateEntry> { entryClaimed, entryUnrelated });
        });

        await using var db = CreateDbContext(interceptor);
        db.Attach(unrelatedMsg);
        db.Entry(unrelatedMsg).State = EntityState.Modified;

        var repo = new OutboxRepository(db);

        // Act & Assert:
        // ClaimMessagesAsync attempts to claim pendingMsg. During SaveChangesAsync, a mixed concurrency exception occurs.
        // ClaimMessagesAsync must detect the mixed entry, propagate the exception, and NOT detach entities.
        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repo.ClaimMessagesAsync(10));
        Assert.Equal(2, ex.Entries.Count);
        Assert.Contains(ex.Entries, e => ReferenceEquals(e.Entity, unrelatedMsg));
        Assert.NotEqual(EntityState.Detached, db.Entry(unrelatedMsg).State);
    }

    [Fact]
    public async Task EmptyConcurrencyExceptionEntries_PropagatesExceptionWithoutSwallowing()
    {
        // Arrange: seed 1 pending message
        var pendingMsg = OutboxMessage.Create("PendingType", "Order", "p3", "c-p3", "{}");
        await using (var seedDb = CreateDbContext())
        {
            await seedDb.OutboxMessages.AddAsync(pendingMsg);
            await seedDb.SaveChangesAsync();
        }

        var interceptor = new ThrowingInterceptor(_ =>
            new DbUpdateConcurrencyException("Synthesized empty entries exception"));

        await using var db = CreateDbContext(interceptor);
        var repo = new OutboxRepository(db);

        // Act & Assert: when ex.Entries is empty, ClaimMessagesAsync must propagate the exception
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repo.ClaimMessagesAsync(10));
    }

    private sealed class ThrowingInterceptor(Func<DbContextEventData, Exception> exceptionFactory) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            throw exceptionFactory(eventData);
        }
    }

    private static Microsoft.EntityFrameworkCore.Update.IUpdateEntry GetUpdateEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var iface = entry.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<>));
        if (iface != null)
        {
            var prop = iface.GetProperty("Instance");
            var val = prop?.GetValue(entry);
            if (val is Microsoft.EntityFrameworkCore.Update.IUpdateEntry updateEntry)
            {
                return updateEntry;
            }
        }

        var internalEntryProp = entry.GetType().GetProperty("InternalEntry", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (Microsoft.EntityFrameworkCore.Update.IUpdateEntry)internalEntryProp!.GetValue(entry)!;
    }

    private sealed class CallbackSavingChangesInterceptor(Func<Task> onSavingAsync) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await onSavingAsync();
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class SpyOutboxMessageHandler(string messageType) : IOutboxMessageHandler
    {
        private int _executionCount;

        public string MessageType => messageType;
        public int ExecutionCount => _executionCount;

        public Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _executionCount);
            return Task.CompletedTask;
        }
    }
}
