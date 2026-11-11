using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using System.Collections.Concurrent;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Infrastructure.Outbox;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Outbox;

public sealed class SqlRestartRecoveryTests
{
    [Fact]
    public async Task NewWorker_RecoversPendingRetryAndAbandonedClaims_WithoutReplayingCompletedOrDeadLetter()
    {
        var database = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(SqlTestDatabaseGuard.ConnectionString(database)).Options;
        await using var db = new RestaurantDbContext(options);
        var deliveries = new ConcurrentDictionary<string, int>();
        try
        {
            await db.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(db);
            OutboxMessage Message(string key) => OutboxMessage.Create(OutboxMessageType.ReceiptPrint, "Order", key, key, "{}", key);
            var pending = Message("pending");
            var retry = Message("retry");
            retry.MarkProcessing(DateTimeOffset.UtcNow.AddMinutes(-10));
            retry.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(-1), "injected printer outage");
            var abandoned = Message("abandoned");
            abandoned.MarkProcessing(DateTimeOffset.UtcNow.AddMinutes(-10));
            var complete = Message("complete");
            complete.MarkCompleted();
            var dead = Message("dead");
            dead.MarkDeadLetter("manual operator decision required");
            var failed = Message("failed");
            failed.MarkFailed("operator retry required");
            db.AddRange(pending, retry, abandoned, complete, dead, failed);
            await db.SaveChangesAsync();

            ServiceProvider NewHost()
            {
                var services = new ServiceCollection();
                services.AddScoped(_ => new RestaurantDbContext(options));
                services.AddScoped<IOutboxRepository, OutboxRepository>();
                services.AddSingleton<IOutboxMessageHandler>(new ReceiptHandler(deliveries));
                return services.BuildServiceProvider();
            }

            using (var host = NewHost())
            using (var worker = new OutboxProcessor(host.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxProcessor>.Instance))
            {
                Assert.Equal(1, await worker.RecoverStaleProcessingMessagesAsync());
                Assert.Equal(3, await worker.ProcessBatchAsync());
            }
            // Recreate both provider and worker against persisted state, without
            // launching the desktop application or pretending this kills a process.
            using (var nextHost = NewHost())
            using (var nextWorker = new OutboxProcessor(nextHost.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxProcessor>.Instance))
            {
                Assert.Equal(0, await nextWorker.RecoverStaleProcessingMessagesAsync());
                Assert.Equal(0, await nextWorker.ProcessBatchAsync());
            }
            await using var verify = new RestaurantDbContext(options);
            var messages = await verify.OutboxMessages.ToListAsync();
            Assert.Equal(6, messages.Count);
            Assert.Equal(4, messages.Count(m => m.Status == OutboxMessageStatus.Completed));
            Assert.Equal("manual operator decision required", Assert.Single(messages, m => m.Status == OutboxMessageStatus.DeadLetter).LastError);
            Assert.Equal("operator retry required", Assert.Single(messages, m => m.Status == OutboxMessageStatus.Failed).LastError);
            Assert.Equal(3, deliveries.Count);
            Assert.All(deliveries.Values, count => Assert.Equal(1, count));
            Assert.Equal(0, await verify.Payments.CountAsync());
            Assert.Equal(0, await verify.Refunds.CountAsync());
        }
        finally { await SqlTestDatabaseGuard.DeleteAndVerifyAsync(db); }
    }

    private sealed class ReceiptHandler(ConcurrentDictionary<string, int> deliveries) : IOutboxMessageHandler
    {
        public string MessageType => OutboxMessageType.ReceiptPrint;
        public Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            deliveries.AddOrUpdate(Assert.IsType<string>(message.IdempotencyKey), 1, (_, count) => count + 1);
            return Task.CompletedTask;
        }
    }
}
