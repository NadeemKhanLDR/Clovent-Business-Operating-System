using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Application.Refunds.Dtos;
using Clovent.Restaurant.Application.Shifts.Queries;
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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Outbox;

/// <summary>
/// Deterministic SQL Server regression test reproducing and verifying the fix for the
/// concurrent refund commit vs. background OutboxProcessor restock message collision.
/// Validates truthful cashier outcome, non-poisoned MediatR queries, and idempotent retries.
/// </summary>
public sealed class RefundOutboxConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly string _databaseName;
    private readonly string _connectionString;
    private readonly ServiceProvider _serviceProvider;
    private readonly DeterministicOutboxSyncInterceptor _interceptor;

    private readonly BranchId _branchId = new(Guid.NewGuid());
    private readonly WarehouseId _warehouseId = new(Guid.NewGuid());
    private readonly TerminalId _terminalId = new(Guid.NewGuid());
    private readonly UserId _cashierId = new(Guid.NewGuid());
    private readonly ProductVariantId _variantId = new(Guid.NewGuid());
    private Guid _shiftId;
    private Guid _orderId;
    private Guid _orderLineId;

    public RefundOutboxConcurrencyIntegrationTests()
    {
        _databaseName = $"CBOS_RefundConcurrency_{Guid.NewGuid():N}";
        _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30;";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString,
                ["ConnectionStrings:Restaurant"] = _connectionString,
                ["ConnectionStrings:Inventory"] = _connectionString
            })
            .Build();

        _interceptor = new DeterministicOutboxSyncInterceptor();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<Clovent.Platform.CircuitBreakers.ICircuitBreakerRegistry, Clovent.Platform.CircuitBreakers.CircuitBreakerRegistry>();
        Clovent.Platform.Sync.SyncServiceCollectionExtensions.AddTestPlatformSync(services);

        // Production DI registrations
        Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(services, configuration);
        Clovent.Restaurant.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(services, configuration);
        Clovent.Restaurant.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(services, configuration);

        Clovent.Inventory.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(services, configuration);
        Clovent.Inventory.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(services, configuration);
        Clovent.Inventory.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(services, configuration);

        // Reconfigure RestaurantDbContext options to include the deterministic interceptor
        var optionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<RestaurantDbContext>));
        if (optionsDescriptor != null) services.Remove(optionsDescriptor);

        services.AddScoped(sp =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<RestaurantDbContext>()
                .UseSqlServer(_connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Restaurant"))
                .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                .AddInterceptors(_interceptor);
            return optionsBuilder.Options;
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        using var initScope = _serviceProvider.CreateScope();
        var restaurantDb = initScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        var inventoryDb = initScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        await restaurantDb.Database.EnsureCreatedAsync();
        var inventoryCreator = (Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator)inventoryDb.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();
        await inventoryCreator.CreateTablesAsync();

        // Seed initial warehouse inventory stock so restock can operate cleanly
        var mediator = initScope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new OpenOrReceiveStockCommand(
            _warehouseId.Value,
            _variantId.Value,
            10m,
            "Initial Stock",
            "Initial",
            Guid.NewGuid()));

        // Seed PaymentMethod
        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        await restaurantDb.PaymentMethods.AddAsync(cashMethod);

        // Seed Shift
        var shift = Shift.Open(
            shiftNumber: 101,
            branchId: _branchId,
            warehouseId: _warehouseId,
            terminalId: _terminalId,
            cashierId: _cashierId,
            cashierName: "Cashier Alice",
            startingCash: 5000m);
        await restaurantDb.Shifts.AddAsync(shift);
        _shiftId = shift.Id.Value;

        // Seed Order with Line and Payment
        var order = Order.Create(OrderType.TakeAway, _warehouseId, null, OrderNumber.Generate(DateTimeOffset.UtcNow));
        var orderLine = OrderLine.Create(order.Id, _variantId, 2m, 500m, 16m, false);
        orderLine.SetTaxSnapshot("Taxable", "PRA", "PK-PRA-16", 1000m, 0m, 160m, "1.3.0-AwayFromZero-v1");
        order.AddOrderLine(orderLine.Id);

        var payment = Payment.Create(order.Id, cashMethod.Id, 1160m, shift.Id);
        order.RecordPayment(payment.Id);
        order.AssignDailySalesNumber(1);
        order.Complete();

        await restaurantDb.Orders.AddAsync(order);
        await restaurantDb.OrderLines.AddAsync(orderLine);
        await restaurantDb.Payments.AddAsync(payment);
        await restaurantDb.SaveChangesAsync();

        _orderId = order.Id.Value;
        _orderLineId = orderLine.Id.Value;
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
            // Suppress teardown errors
        }

        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task Refund_WhenBackgroundWorkerUpdatesOutboxConcurrently_MustSucceedAndPreserveQueryAndRetry()
    {
        // Arrange: Coordinate execution so worker processes outbox message between refund commit and post-commit update
        var workerExecuted = false;
        _interceptor.OnRefundSavedAsync = async () =>
        {
            var processor = _serviceProvider.GetRequiredService<IOutboxProcessor>();
            var processedCount = await processor.ProcessBatchAsync();
            workerExecuted = processedCount > 0;
        };

        var idempotencyKey = $"IDEMP-REFUND-{Guid.NewGuid():N}";
        var refundCommand = new ProcessRefundCommand(
            OrderId: _orderId,
            Items: [new RefundItemRequest(_orderLineId, 1m, InventoryDisposition.Restock)],
            Reason: "Customer Return",
            SettlementMethod: RefundSettlementMethod.CashPayout,
            IdempotencyKey: idempotencyKey,
            CashierId: _cashierId.Value,
            CashierName: "Cashier Alice",
            BranchId: _branchId.Value,
            ApprovingUserId: _cashierId.Value,
            ApprovingUserName: "Cashier Alice",
            ShiftId: _shiftId,
            TerminalId: _terminalId.Value);

        using var sessionScope = _serviceProvider.CreateScope();
        var mediator = sessionScope.ServiceProvider.GetRequiredService<IMediator>();

        // Act 1: Process refund under concurrent outbox worker processing
        var refundDto = await mediator.Send(refundCommand);

        // Assert 1: Refund must succeed with valid DTO
        Assert.True(workerExecuted, "Background outbox worker must have executed concurrently upon refund commit.");
        Assert.NotNull(refundDto);
        Assert.Equal(_orderId, refundDto.OrderId);
        Assert.Equal(580m, refundDto.GrandTotalRefunded); // 500 base + 80 tax

        // Act & Assert 2: Subsequent read-only query in same session scope must not be poisoned by pending changes
        var lastCompletedOrder = await mediator.Send(new GetLastCompletedOrderForShiftQuery(_shiftId));
        Assert.NotNull(lastCompletedOrder);
        Assert.Equal(_orderId, lastCompletedOrder.OrderId);

        // Act & Assert 3: Retrying same refund request must return existing refund idempotently without failure or duplication
        var retriedDto = await mediator.Send(refundCommand);
        Assert.NotNull(retriedDto);
        Assert.Equal(refundDto.RefundId, retriedDto.RefundId);
        Assert.Equal(refundDto.RefundNumber, retriedDto.RefundNumber);

        // Assert 4: Verify exactly-once persistence in database
        using var verifyScope = _serviceProvider.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        var refunds = await verifyDb.Refunds.Where(r => r.OrderId == new OrderId(_orderId)).ToListAsync();
        Assert.Single(refunds);

        var outboxMessages = await verifyDb.OutboxMessages
            .Where(m => m.AggregateType == "Refund" && m.AggregateId == refunds[0].Id.Value.ToString())
            .ToListAsync();
        Assert.Single(outboxMessages);
        Assert.Equal(OutboxMessageStatus.Completed, outboxMessages[0].Status);
    }

    private sealed class DeterministicOutboxSyncInterceptor : SaveChangesInterceptor
    {
        public Func<Task>? OnRefundSavedAsync { get; set; }
        private int _refundSavedCount;

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is RestaurantDbContext dbContext)
            {
                var hasRefund = dbContext.ChangeTracker.Entries<Refund>().Any();
                if (hasRefund && Interlocked.Increment(ref _refundSavedCount) == 1)
                {
                    if (OnRefundSavedAsync != null)
                    {
                        await OnRefundSavedAsync();
                    }
                }
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }
    }
}
