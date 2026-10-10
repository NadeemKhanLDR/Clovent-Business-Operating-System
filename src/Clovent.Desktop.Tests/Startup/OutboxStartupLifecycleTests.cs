using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Infrastructure.DependencyInjection;
using Clovent.Restaurant.Infrastructure.Outbox;
using Clovent.Restaurant.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Clovent.Desktop.Tests.Startup;

/// <summary>
/// Verifies Task-07: Outbox Processor automated lifecycle management.
/// Guarantees that OutboxProcessor starts automatically upon application boot
/// via the Microsoft.Extensions.Hosting IHostedService pipeline without requiring
/// manual trigger from OperationsHealthForm, and terminates gracefully on host shutdown.
/// </summary>
public sealed class OutboxStartupLifecycleTests
{
    private sealed class TestOutboxRepository : IOutboxRepository
    {
        private readonly Dictionary<OutboxMessageId, OutboxMessage> _store = [];

        public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            _store[message.Id] = message;
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
        {
            foreach (var m in messages) _store[m.Id] = m;
            return Task.CompletedTask;
        }

        public Task<OutboxMessage?> GetByIdAsync(OutboxMessageId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.GetValueOrDefault(id));

        public Task<OutboxMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.Values.FirstOrDefault(m => m.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<OutboxMessage>> ClaimMessagesAsync(int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxMessage>>([]);

        public Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            _store[message.Id] = message;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OutboxMessage>> GetStaleProcessingMessagesAsync(TimeSpan staleThreshold, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxMessage>>([]);

        public Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new OutboxStatistics(0, 0, 0, 0, 0, 0, null, new Dictionary<string, int>()));

        public Task<IReadOnlyList<OutboxMessage>> GetDeadLetterAndFailedMessagesAsync(int maxCount = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxMessage>>([]);

        public Task<IReadOnlyList<OutboxMessage>> GetUncompletedMessagesByTypeAsync(string messageType, int maxCount = 100, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
    }

    private static IHost CreateTestHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddLogging();
        builder.Services.AddSingleton<IOutboxRepository, TestOutboxRepository>();
        Clovent.Restaurant.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(builder.Services, builder.Configuration);

        return builder.Build();
    }

    [Fact]
    public void OutboxProcessor_IsRegisteredAsHostedService_AndResolvesSameSingletonInstance()
    {
        using var host = CreateTestHost();

        var outboxProcessor = host.Services.GetRequiredService<IOutboxProcessor>();
        var hostedServices = host.Services.GetServices<IHostedService>().ToList();

        Assert.NotNull(outboxProcessor);
        Assert.Contains(hostedServices, s => ReferenceEquals(s, outboxProcessor));
    }

    [Fact]
    public async Task OutboxProcessor_StartsAutomatically_WhenHostStarts()
    {
        using var host = CreateTestHost();
        var outboxProcessor = host.Services.GetRequiredService<IOutboxProcessor>();

        Assert.False(outboxProcessor.IsRunning, "OutboxProcessor must not be running prior to host startup.");

        await host.StartAsync();

        Assert.True(outboxProcessor.IsRunning, "OutboxProcessor must start automatically when IHost starts.");

        await host.StopAsync();
        Assert.False(outboxProcessor.IsRunning, "OutboxProcessor must stop when IHost stops.");
    }

    [Fact]
    public async Task OutboxProcessor_StopsGracefully_WithoutDeadlockOrThreadLeak()
    {
        using var host = CreateTestHost();
        var outboxProcessor = host.Services.GetRequiredService<IOutboxProcessor>();

        await host.StartAsync();
        Assert.True(outboxProcessor.IsRunning);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stopTask = host.StopAsync(cts.Token);

        // Ensure StopAsync completes within timeout without deadlocks
        var completed = await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(6))) == stopTask;
        Assert.True(completed, "Host shutdown must complete gracefully within deadline without deadlock.");
        Assert.False(outboxProcessor.IsRunning, "OutboxProcessor must report IsRunning = false after shutdown.");
    }
}
