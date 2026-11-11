using Clovent.Identity.Branches;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Execution;
using Clovent.Restaurant.Application;
using Clovent.Restaurant.Application.Concurrency;
using Clovent.Restaurant.Application.Continuity;
using Clovent.Restaurant.Application.DependencyInjection;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Infrastructure.DependencyInjection;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Refunds;
using Clovent.Restaurant.Shifts;
using Clovent.Restaurant.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Startup;

/// <summary>
/// Verifies production dependency guarantees across the Desktop composition root pattern
/// (Application -> Infrastructure -> Persistence) and fail-closed guards on unsafe fallbacks.
/// </summary>
public sealed class ProductionCompositionGuaranteeTests
{
    [Fact]
    public void ProductionComposition_RegistersRequiredTransactionOutboxAndPipelineBehaviorsInOrder()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=localhost;Database=Placeholder;Trusted_Connection=True;"
            })
            .Build();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IExecutionContextAccessor, ExecutionContextAccessor>();
        services.AddSingleton<Clovent.Platform.CircuitBreakers.ICircuitBreakerRegistry, Clovent.Platform.CircuitBreakers.CircuitBreakerRegistry>();
        Clovent.Platform.Sync.SyncServiceCollectionExtensions.AddTestPlatformSync(services);

        // Mirror Clovent.Desktop/Program.cs and Tools/AcceptanceViewRunner/Program.cs
        services.AddApplication(config);
        services.AddInfrastructure(config);
        services.AddPersistence(config);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.NotNull(sp.GetRequiredService<IUnitOfWork>());
        Assert.NotNull(sp.GetRequiredService<IOutboxRepository>());
        Assert.NotNull(sp.GetRequiredService<IOutboxProcessor>());
        Assert.NotNull(sp.GetRequiredService<IShiftRepository>());
        Assert.NotNull(sp.GetRequiredService<IRefundRepository>());
        Assert.NotNull(sp.GetRequiredService<ICustomerLedgerEntryRepository>());
        Assert.NotNull(sp.GetRequiredService<ICustomerPaymentAllocationRepository>());
        Assert.NotNull(sp.GetRequiredService<IPaymentMethodRepository>());
        Assert.IsType<SqlTransactionLockProvider>(sp.GetRequiredService<ITransactionLockProvider>());
        Assert.NotNull(sp.GetRequiredService<IEmergencyJournalReplayer>());
        Assert.NotNull(sp.GetRequiredService<IRequestHandler<CompleteOrderCommand, Clovent.Restaurant.Application.Orders.Dtos.OrderDto>>());
        Assert.NotNull(sp.GetRequiredService<IRequestHandler<ProcessRefundCommand, Clovent.Restaurant.Application.Refunds.Dtos.RefundDto>>());

        // Verify MediatR pipeline ordering: ScopeEnforcement -> TransactionSerialization -> UnitOfWork
        var behaviors = sp.GetServices<IPipelineBehavior<RecordPaymentCommand, Clovent.Restaurant.Application.Payments.Dtos.PaymentDto>>()
            .Select(b => b.GetType().GetGenericTypeDefinition())
            .ToList();

        var scopeIdx = behaviors.IndexOf(typeof(ScopeEnforcementBehavior<,>));
        var lockIdx = behaviors.IndexOf(typeof(TransactionSerializationBehavior<,>));
        var uowIdx = behaviors.IndexOf(typeof(UnitOfWorkBehavior<,>));

        Assert.True(scopeIdx >= 0, "ScopeEnforcementBehavior must be registered.");
        Assert.True(lockIdx > scopeIdx, "TransactionSerializationBehavior must run inside ScopeEnforcementBehavior.");
        Assert.True(uowIdx > lockIdx, "TransactionSerializationBehavior must run outer to UnitOfWorkBehavior so locks stay held through SaveChangesAsync.");
    }

    [Fact]
    public async Task CompleteOrderCommandHandler_OutboxProcessorWithoutOutboxRepository_FailsClosed()
    {
        var handler = new CompleteOrderCommandHandler(
            new DummyOrderRepo(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            outboxRepository: null,
            outboxProcessor: new DummyOutboxProcessor());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CompleteOrderCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ProcessRefundCommandHandler_OutboxProcessorWithoutOutboxRepository_FailsClosed()
    {
        var handler = new ProcessRefundCommandHandler(
            null!,
            new DummyOrderRepo(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            outboxRepository: null,
            outboxProcessor: new DummyOutboxProcessor());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new ProcessRefundCommand(
                    Guid.NewGuid(),
                    [new RefundItemRequest(Guid.NewGuid(), 1m, InventoryDisposition.Restock)],
                    "Defective",
                    RefundSettlementMethod.CashPayout,
                    "IDEM-FAIL-CLOSED-1",
                    Guid.NewGuid(),
                    "Cashier"),
                CancellationToken.None));
    }

    [Fact]
    public async Task EmergencyJournalReplayer_WithoutOutboxRepository_FailsClosedInsteadOfDroppingInventory()
    {
        var store = new SinglePendingJournalStore();
        var replayer = new EmergencyJournalReplayer(
            store,
            null!,
            null!,
            null!,
            null!,
            null!,
            outboxRepository: null,
            transactionLockProvider: new DummyTransactionLockProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(() => replayer.ReplayPendingAsync());
    }

    [Fact]
    public void EmergencyJournalReplayer_NullTransactionLockProvider_FailsClosed()
    {
        var store = new SinglePendingJournalStore();

        Assert.Throws<ArgumentNullException>(() => new EmergencyJournalReplayer(
            store,
            null!,
            null!,
            null!,
            null!,
            null!,
            outboxRepository: new DummyOutboxRepository(),
            transactionLockProvider: null!,
            outboxProcessor: new DummyOutboxProcessor()));
    }

    [Fact]
    public void EmergencyJournalReplayer_MissingTransactionLockProviderInDI_FailsClosedAtResolution()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=localhost;Database=Placeholder;Trusted_Connection=True;"
            })
            .Build();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IExecutionContextAccessor, ExecutionContextAccessor>();
        services.AddSingleton<Clovent.Platform.CircuitBreakers.ICircuitBreakerRegistry, Clovent.Platform.CircuitBreakers.CircuitBreakerRegistry>();
        Clovent.Platform.Sync.SyncServiceCollectionExtensions.AddTestPlatformSync(services);

        services.AddApplication(config);
        services.AddInfrastructure(config);
        services.AddPersistence(config);

        // Simulate incomplete composition missing ITransactionLockProvider
        var descriptor = services.First(d => d.ServiceType == typeof(ITransactionLockProvider));
        services.Remove(descriptor);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IEmergencyJournalReplayer>());
    }

    private sealed class DummyTransactionLockProvider : ITransactionLockProvider
    {
        public Task<T> ExecuteAsync<T>(IReadOnlyList<TransactionLockKey> keys, CancellationToken cancellationToken, Func<Task<T>> action) =>
            action();
    }

    private sealed class DummyOutboxRepository : IOutboxRepository
    {
        public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRangeAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<OutboxMessage?> GetByIdAsync(OutboxMessageId id, CancellationToken cancellationToken = default) => Task.FromResult<OutboxMessage?>(null);
        public Task<OutboxMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) => Task.FromResult<OutboxMessage?>(null);
        public Task<IReadOnlyList<OutboxMessage>> ClaimMessagesAsync(int batchSize, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        public Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OutboxMessage>> GetStaleProcessingMessagesAsync(TimeSpan staleThreshold, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        public Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new OutboxStatistics(0, 0, 0, 0, 0, 0, null, new Dictionary<string, int>()));
        public Task<IReadOnlyList<OutboxMessage>> GetDeadLetterAndFailedMessagesAsync(int maxCount = 50, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        public Task<IReadOnlyList<OutboxMessage>> GetUncompletedMessagesByTypeAsync(string messageType, int maxCount = 100, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
    }

    private sealed class DummyOutboxProcessor : IOutboxProcessor
    {
        public bool IsRunning => true;
        public Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public void TriggerImmediate() { }
        public Task<int> RecoverStaleProcessingMessagesAsync(TimeSpan? staleThreshold = null, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public event EventHandler<int>? BatchCompleted { add { } remove { } }
    }

    private sealed class DummyOrderRepo : IOrderRepository
    {
        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
        public Task<Order?> GetByOrderNumberAsync(Clovent.Restaurant.Orders.ValueObjects.OrderNumber orderNumber, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
        public Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Order>>([]);
        public Task<IReadOnlyCollection<Order>> GetOpenOrHeldByTableIdAsync(TableId tableId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Order>>([]);
        public Task<IReadOnlySet<TableId>> GetActiveTableIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<TableId>>(new HashSet<TableId>());
        public Task<IReadOnlyCollection<Order>> GetOpenAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Order>>([]);
        public Task<IReadOnlyCollection<Order>> GetHeldAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Order>>([]);
        public Task<IReadOnlyCollection<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Order>>([]);
        public Task AddAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SinglePendingJournalStore : Clovent.Restaurant.Continuity.IContinuityJournalStore
    {
        public Task AppendAsync(Clovent.Restaurant.Continuity.EmergencyTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Clovent.Restaurant.Continuity.EmergencyTransaction>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Clovent.Restaurant.Continuity.EmergencyTransaction>>([]);
        public Task<IReadOnlyList<Clovent.Restaurant.Continuity.EmergencyTransaction>> GetPendingReplayAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Clovent.Restaurant.Continuity.EmergencyTransaction>>([new Clovent.Restaurant.Continuity.EmergencyTransaction()]);
        public Task UpdateAsync(Clovent.Restaurant.Continuity.EmergencyTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Clovent.Restaurant.Continuity.ContinuityJournalStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Clovent.Restaurant.Continuity.ContinuityJournalStatistics(1, 1, 0, 0, DateTimeOffset.UtcNow));
        public Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default) => Task.FromResult(1L);
        public Task<string> GetLastTransactionHashAsync(CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
    }
}
