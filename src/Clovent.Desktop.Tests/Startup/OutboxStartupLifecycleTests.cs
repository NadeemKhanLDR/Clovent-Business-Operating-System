using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Security;
using Clovent.Platform.Execution;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Infrastructure.DependencyInjection;
using Clovent.Restaurant.Infrastructure.Outbox;
using Clovent.Restaurant.Outbox;
using MediatR;
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

        public Task<IReadOnlyList<OutboxMessage>> ClaimMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var pending = _store.Values.Where(m => m.Status == OutboxMessageStatus.Pending).Take(batchSize).ToList();
            foreach (var m in pending)
            {
                m.ClaimForProcessing();
            }
            return Task.FromResult<IReadOnlyList<OutboxMessage>>(pending);
        }

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

    private sealed class FakeUnitOfWork : Clovent.Restaurant.Application.IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static IHost CreateTestHost(Action<IServiceCollection>? configureServices = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddLogging();
        builder.Services.AddSingleton<IOutboxRepository, TestOutboxRepository>();
        builder.Services.AddScoped<Clovent.Restaurant.Application.IUnitOfWork, FakeUnitOfWork>();
        Clovent.Restaurant.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(builder.Services, builder.Configuration);

        configureServices?.Invoke(builder.Services);

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

    [Fact]
    public async Task OutboxProcessor_WhileCashierAuthenticated_DispatchesWithoutAdoptingOrConflictingWithCashierScope()
    {
        using var host = CreateTestHost(services =>
        {
            services.AddScopeEnforcement();
            services.AddSingleton<IInteractiveScopeProvider, DesktopInteractiveScopeProvider>();
            services.AddSingleton<TestUnscopedOutboxHandler>();
            services.AddSingleton<TestScopedOutboxHandler>();
            services.AddScoped<IOutboxMessageHandler>(sp => sp.GetRequiredService<TestUnscopedOutboxHandler>());
            services.AddScoped<IOutboxMessageHandler>(sp => sp.GetRequiredService<TestScopedOutboxHandler>());
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(OutboxStartupLifecycleTests).Assembly);
            });
        });

        var interactiveProvider = host.Services.GetRequiredService<IInteractiveScopeProvider>();
        var outboxRepo = (TestOutboxRepository)host.Services.GetRequiredService<IOutboxRepository>();
        var outboxProcessor = host.Services.GetRequiredService<IOutboxProcessor>();
        var unscopedHandler = host.Services.GetRequiredService<TestUnscopedOutboxHandler>();
        var scopedHandler = host.Services.GetRequiredService<TestScopedOutboxHandler>();

        // 1. Authenticate a cashier in the desktop interactive session
        var cashierUser = Guid.NewGuid();
        var cashierOrg = Guid.NewGuid();
        var cashierComp = Guid.NewGuid();
        var cashierBranch = Guid.NewGuid();

        var cashierContext = new PlatformExecutionContext
        {
            UserId = cashierUser,
            OrganizationId = cashierOrg,
            CompanyId = cashierComp,
            BranchId = cashierBranch
        };
        interactiveProvider.SetCurrentContext(cashierContext);

        Assert.NotNull(interactiveProvider.GetCurrentContext());
        Assert.Equal(cashierUser, interactiveProvider.GetCurrentContext()?.UserId);

        // 2. Enqueue two messages:
        // Message 1: Unscoped message. Inside handler, checks if accessor adopted cashier scope.
        var msg1 = OutboxMessage.Create("TestUnscoped", "TestAggregate", Guid.NewGuid().ToString(), "corr-1", "{}");
        await outboxRepo.AddAsync(msg1);

        // Message 2: Scoped message. Inside handler, establishes legitimate worker scope for another branch and sends MediatR command.
        var msg2 = OutboxMessage.Create("TestScoped", "TestAggregate", Guid.NewGuid().ToString(), "corr-2", "{}");
        await outboxRepo.AddAsync(msg2);

        // 3. Process the batch via OutboxProcessor
        var processedCount = await outboxProcessor.ProcessBatchAsync();
        Assert.Equal(2, processedCount);

        // 4. Verify message outcomes
        Assert.True(unscopedHandler.Executed, "Unscoped worker message must execute.");
        Assert.False(unscopedHandler.AdoptedCashierScope, "Worker must NOT silently adopt cashier's scope when un-scoped.");

        Assert.True(scopedHandler.Executed, "Scoped worker message must execute.");
        Assert.Null(scopedHandler.CaughtException?.ToString());
        Assert.True(scopedHandler.ExecutedSuccessfully, "Worker with legitimate scope must succeed without conflicting with cashier session.");
        Assert.Equal($"Processed at branch {TestScopedOutboxHandler.WorkerBranchId}", scopedHandler.Result);

        // 5. Verify the cashier's interactive session is intact and uncorrupted
        Assert.NotNull(interactiveProvider.GetCurrentContext());
        Assert.Equal(cashierUser, interactiveProvider.GetCurrentContext()?.UserId);
        Assert.Equal(cashierBranch, interactiveProvider.GetCurrentContext()?.BranchId);
    }
}

public sealed record WorkerTestBranchCommand(string Item) : IRequest<string>, IRequireBranchScope;

public sealed class WorkerTestBranchCommandHandler(IExecutionContextAccessor accessor)
    : IRequestHandler<WorkerTestBranchCommand, string>
{
    public Task<string> Handle(WorkerTestBranchCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Processed at branch {accessor.Current?.BranchId}");
    }
}

public sealed class TestUnscopedOutboxHandler(IExecutionContextAccessor accessor) : IOutboxMessageHandler
{
    public string MessageType => "TestUnscoped";
    public bool Executed { get; private set; }
    public bool AdoptedCashierScope { get; private set; }

    public Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        Executed = true;
        if (accessor.Current != null)
        {
            AdoptedCashierScope = true;
        }
        return Task.CompletedTask;
    }
}

public sealed class TestScopedOutboxHandler(
    IMediator mediator,
    IBackgroundExecutionScopeService bgScopeService) : IOutboxMessageHandler
{
    public static readonly Guid WorkerOrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid WorkerCompId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid WorkerBranchId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public string MessageType => "TestScoped";
    public bool Executed { get; private set; }
    public bool ExecutedSuccessfully { get; private set; }
    public string? Result { get; private set; }
    public Exception? CaughtException { get; private set; }

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        Executed = true;
        try
        {
            using (bgScopeService.EstablishScope(WorkerOrgId, WorkerCompId, WorkerBranchId, null, "WorkerJob"))
            {
                Result = await mediator.Send(new WorkerTestBranchCommand("Item1"), cancellationToken);
                ExecutedSuccessfully = true;
            }
        }
        catch (Exception ex)
        {
            CaughtException = ex;
            throw;
        }
    }
}
