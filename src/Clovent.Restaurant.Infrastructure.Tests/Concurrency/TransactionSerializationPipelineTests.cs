using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Identity.Branches;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Execution;
using Clovent.Restaurant.Application;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.DependencyInjection;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Infrastructure.DependencyInjection;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Refunds;
using Clovent.Restaurant.Shifts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Concurrency;

/// <summary>
/// Reproduces and verifies persistence-boundary concurrency guarantees across independent DI scopes:
/// 1. Locks acquired by MediatR transaction commands must remain held until UnitOfWorkBehavior finishes SaveChangesAsync().
/// 2. Concurrent customer balance/credit/advance mutations on DIFFERENT orders for the SAME customer must serialize.
/// 3. Shift closure (CloseShiftCommand) racing with payments, customer collections, or cash refunds must serialize
///    through commit and reject post-close mutations deterministically.
/// </summary>
public sealed class TransactionSerializationPipelineTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public TransactionSerializationPipelineTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cbos-tx-pipeline-{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath};Default Timeout=10";
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch
        {
        }
    }

    private sealed class BarrierControl
    {
        private readonly object _gate = new();
        private TaskCompletionSource<bool>? _entered;
        private TaskCompletionSource<bool>? _release;
        private int _armed;

        public void ArmOnce()
        {
            lock (_gate)
            {
                _entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _armed = 1;
            }
        }

        public Task WaitUntilSaveEnteredAsync()
        {
            lock (_gate)
            {
                return _entered?.Task ?? Task.CompletedTask;
            }
        }

        public void ReleaseSave()
        {
            lock (_gate)
            {
                _release?.TrySetResult(true);
            }
        }

        public async Task PauseIfArmedAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool>? entered = null;
            TaskCompletionSource<bool>? release = null;
            lock (_gate)
            {
                if (_armed == 1)
                {
                    _armed = 0;
                    entered = _entered;
                    release = _release;
                }
            }

            if (entered is not null && release is not null)
            {
                entered.TrySetResult(true);
                await release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class BarrierUnitOfWork(IUnitOfWork inner, BarrierControl barrier) : IUnitOfWork
    {
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await barrier.PauseIfArmedAsync(cancellationToken).ConfigureAwait(false);
            await inner.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private ServiceProvider BuildTestServiceProvider(BarrierControl barrier)
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Restaurant"] = "Server=localhost;Database=Placeholder;Trusted_Connection=True;"
            })
            .Build();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IExecutionContextAccessor, ExecutionContextAccessor>();
        services.AddSingleton<Clovent.Platform.CircuitBreakers.ICircuitBreakerRegistry, Clovent.Platform.CircuitBreakers.CircuitBreakerRegistry>();
        Clovent.Platform.Sync.SyncServiceCollectionExtensions.AddTestPlatformSync(services);

        // Production registration order: Application -> Infrastructure -> Persistence
        services.AddApplication(config);
        services.AddInfrastructure(config);
        services.AddPersistence(config);

        // Replace DbContext with isolated temp SQLite file (without mixing EF Core providers on app DI) & wrap IUnitOfWork with save barrier
        services.RemoveAll<DbContextOptions<RestaurantDbContext>>();
        services.RemoveAll<DbContextOptions>();
        services.RemoveAll<RestaurantDbContext>();
        var sqliteOptions = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlite(_connectionString).Options;
        services.AddSingleton(sqliteOptions);
        services.AddSingleton<DbContextOptions>(sqliteOptions);
        services.AddScoped(sp => new RestaurantDbContext(sp.GetRequiredService<DbContextOptions<RestaurantDbContext>>()));

        services.RemoveAll<IUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp =>
            new BarrierUnitOfWork(new Clovent.Restaurant.Infrastructure.Persistence.UnitOfWork(sp.GetRequiredService<RestaurantDbContext>()), barrier));

        var sp = services.BuildServiceProvider();
        using var initScope = sp.CreateScope();
        initScope.ServiceProvider.GetRequiredService<RestaurantDbContext>().Database.EnsureCreated();
        return sp;
    }

    [Fact]
    public async Task RecordPayment_IndependentScopes_BarrierInsideSaveChanges_SerializesUntilCommitted()
    {
        var barrier = new BarrierControl();
        await using var sp = BuildTestServiceProvider(barrier);

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        var line = OrderLine.Create(order.Id, ProductVariantId.New(), 1m, 500m, 0m, false);
        order.AddOrderLine(line.Id);

        using (var seedScope = sp.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            await db.PaymentMethods.AddAsync(cashMethod);
            await db.Orders.AddAsync(order);
            await db.OrderLines.AddAsync(line);
            await db.SaveChangesAsync();
        }

        barrier.ArmOnce();

        var scope1Task = Task.Run(async () =>
        {
            using var scope1 = sp.CreateScope();
            var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
            return await mediator1.Send(new RecordPaymentCommand(order.Id.Value, cashMethod.Id.Value, 500m));
        });

        // Wait until Scope 1's handler has returned and UnitOfWorkBehavior is paused right BEFORE SaveChangesAsync commits
        await barrier.WaitUntilSaveEnteredAsync();

        var scope2Task = Task.Run(async () =>
        {
            using var scope2 = sp.CreateScope();
            var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();
            return await mediator2.Send(new RecordPaymentCommand(order.Id.Value, cashMethod.Id.Value, 500m));
        });

        // Under outer transaction locking, Scope 2 must stay blocked until Scope 1 commits.
        var finishedEarly = await Task.WhenAny(scope2Task, Task.Delay(150)) == scope2Task;
        barrier.ReleaseSave();

        await scope1Task;

        Assert.False(finishedEarly, "Scope 2 entered before Scope 1 persisted via SaveChangesAsync().");
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope2Task);

        using var verifyScope = sp.CreateScope();
        var payments = await verifyScope.ServiceProvider.GetRequiredService<IPaymentRepository>()
            .GetByOrderIdAsync(order.Id);
        Assert.Single(payments);
    }

    [Fact]
    public async Task DifferentOrdersSameCustomer_ConcurrentOnAccountCredit_EnforcesCreditLimitStrictly()
    {
        var barrier = new BarrierControl();
        await using var sp = BuildTestServiceProvider(barrier);

        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        var customer = Customer.Create(EntityCode.Create("CUST-01"), "Credit Race Customer", "03001112233", "Street 1", null, 0m, 1000m, null);
        var order1 = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order1.SetCustomer(customer.Id);
        var line1 = OrderLine.Create(order1.Id, ProductVariantId.New(), 1m, 800m, 0m, false);
        order1.AddOrderLine(line1.Id);

        var order2 = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order2.SetCustomer(customer.Id);
        var line2 = OrderLine.Create(order2.Id, ProductVariantId.New(), 1m, 800m, 0m, false);
        order2.AddOrderLine(line2.Id);

        using (var seedScope = sp.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            await db.PaymentMethods.AddAsync(onAccountMethod);
            await db.Customers.AddAsync(customer);
            await db.Orders.AddRangeAsync(order1, order2);
            await db.OrderLines.AddRangeAsync(line1, line2);
            await db.SaveChangesAsync();
        }

        barrier.ArmOnce();

        var scope1Task = Task.Run(async () =>
        {
            using var s1 = sp.CreateScope();
            var m1 = s1.ServiceProvider.GetRequiredService<IMediator>();
            return await m1.Send(new RecordPaymentCommand(order1.Id.Value, onAccountMethod.Id.Value, 800m));
        });

        await barrier.WaitUntilSaveEnteredAsync();

        var scope2Task = Task.Run(async () =>
        {
            using var s2 = sp.CreateScope();
            var m2 = s2.ServiceProvider.GetRequiredService<IMediator>();
            return await m2.Send(new RecordPaymentCommand(order2.Id.Value, onAccountMethod.Id.Value, 800m));
        });

        var finishedEarly = await Task.WhenAny(scope2Task, Task.Delay(150)) == scope2Task;
        barrier.ReleaseSave();

        await scope1Task;

        Assert.False(finishedEarly, "Second order for same customer bypassed customer lock while first was saving.");
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope2Task);

        using var verifyScope = sp.CreateScope();
        var updatedCustomer = await verifyScope.ServiceProvider.GetRequiredService<ICustomerRepository>()
            .GetByIdAsync(customer.Id);
        Assert.NotNull(updatedCustomer);
        Assert.Equal(800m, updatedCustomer.OutstandingBalance);
    }

    [Fact]
    public async Task DifferentOrdersSameCustomer_ConcurrentCustomerAdvance_PreventsDoubleSpend()
    {
        var barrier = new BarrierControl();
        await using var sp = BuildTestServiceProvider(barrier);

        var advanceMethod = PaymentMethod.Create(PaymentMethodName.Create("Customer Advance"));
        var customer = Customer.Create(EntityCode.Create("CUST-ADV"), "Advance Race Customer", "03002223344", "Street 2", null, 0m, 0m, null);
        customer.AdjustBalance(-500m); // 500 advance credit

        var order1 = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order1.SetCustomer(customer.Id);
        var line1 = OrderLine.Create(order1.Id, ProductVariantId.New(), 1m, 500m, 0m, false);
        order1.AddOrderLine(line1.Id);

        var order2 = Order.Create(OrderType.TakeAway, WarehouseId.New());
        order2.SetCustomer(customer.Id);
        var line2 = OrderLine.Create(order2.Id, ProductVariantId.New(), 1m, 500m, 0m, false);
        order2.AddOrderLine(line2.Id);

        using (var seedScope = sp.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            await db.PaymentMethods.AddAsync(advanceMethod);
            await db.Customers.AddAsync(customer);
            await db.Orders.AddRangeAsync(order1, order2);
            await db.OrderLines.AddRangeAsync(line1, line2);
            await db.SaveChangesAsync();
        }

        barrier.ArmOnce();

        var scope1Task = Task.Run(async () =>
        {
            using var s1 = sp.CreateScope();
            return await s1.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new RecordPaymentCommand(order1.Id.Value, advanceMethod.Id.Value, 500m));
        });

        await barrier.WaitUntilSaveEnteredAsync();

        var scope2Task = Task.Run(async () =>
        {
            using var s2 = sp.CreateScope();
            return await s2.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new RecordPaymentCommand(order2.Id.Value, advanceMethod.Id.Value, 500m));
        });

        var finishedEarly = await Task.WhenAny(scope2Task, Task.Delay(150)) == scope2Task;
        barrier.ReleaseSave();

        await scope1Task;

        Assert.False(finishedEarly, "Second order spent customer advance concurrently before first committed.");
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope2Task);

        using var verifyScope = sp.CreateScope();
        var updatedCustomer = await verifyScope.ServiceProvider.GetRequiredService<ICustomerRepository>()
            .GetByIdAsync(customer.Id);
        Assert.NotNull(updatedCustomer);
        Assert.Equal(0m, updatedCustomer.OutstandingBalance);
    }

    [Fact]
    public async Task CloseShift_Vs_RecordPayment_BarrierInsideSaveChanges_RejectsPaymentOnClosingShift()
    {
        var barrier = new BarrierControl();
        await using var sp = BuildTestServiceProvider(barrier);

        var branchId = BranchId.New();
        var warehouseId = WarehouseId.New();
        var terminalId = TerminalId.New();
        var userId = Clovent.Identity.Users.UserId.New();
        var shift = Shift.Open(1, branchId, warehouseId, terminalId, userId, "Cashier", 1000m);

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var order = Order.Create(OrderType.TakeAway, warehouseId);
        var line = OrderLine.Create(order.Id, ProductVariantId.New(), 1m, 400m, 0m, false);
        order.AddOrderLine(line.Id);

        using (var seedScope = sp.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            await db.Shifts.AddAsync(shift);
            await db.PaymentMethods.AddAsync(cashMethod);
            await db.Orders.AddAsync(order);
            await db.OrderLines.AddAsync(line);
            await db.SaveChangesAsync();
        }

        barrier.ArmOnce();

        var closeTask = Task.Run(async () =>
        {
            using var s1 = sp.CreateScope();
            return await s1.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new CloseShiftCommand(shift.Id.Value, 1000m));
        });

        await barrier.WaitUntilSaveEnteredAsync();

        var payTask = Task.Run(async () =>
        {
            using var s2 = sp.CreateScope();
            return await s2.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new RecordPaymentCommand(order.Id.Value, cashMethod.Id.Value, 400m, ShiftId: shift.Id.Value));
        });

        var finishedEarly = await Task.WhenAny(payTask, Task.Delay(150)) == payTask;
        barrier.ReleaseSave();

        await closeTask;

        Assert.False(finishedEarly, "Payment posted to shift while CloseShift was saving.");
        await Assert.ThrowsAsync<InvalidOperationException>(() => payTask);

        using var verifyScope = sp.CreateScope();
        var shiftPayments = await verifyScope.ServiceProvider.GetRequiredService<IPaymentRepository>()
            .GetByShiftIdAsync(shift.Id);
        Assert.Empty(shiftPayments);
    }

    [Fact]
    public async Task CloseShift_Vs_RecordCustomerPayment_BarrierInsideSaveChanges_RejectsCollectionOnClosingShift()
    {
        var barrier = new BarrierControl();
        await using var sp = BuildTestServiceProvider(barrier);

        var shift = Shift.Open(2, BranchId.New(), WarehouseId.New(), TerminalId.New(), Clovent.Identity.Users.UserId.New(), "Cashier", 500m);
        var customer = Customer.Create(EntityCode.Create("CUST-COL"), "Collection Customer", "03003334455", "Street 3", null, 500m, 1000m, null);

        using (var seedScope = sp.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            await db.Shifts.AddAsync(shift);
            await db.Customers.AddAsync(customer);
            await db.SaveChangesAsync();
        }

        barrier.ArmOnce();

        var closeTask = Task.Run(async () =>
        {
            using var s1 = sp.CreateScope();
            return await s1.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new CloseShiftCommand(shift.Id.Value, 500m));
        });

        await barrier.WaitUntilSaveEnteredAsync();

        var collectTask = Task.Run(async () =>
        {
            using var s2 = sp.CreateScope();
            return await s2.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new RecordCustomerPaymentCommand(customer.Id.Value, 200m, "Cash", ShiftId: shift.Id.Value));
        });

        var finishedEarly = await Task.WhenAny(collectTask, Task.Delay(150)) == collectTask;
        barrier.ReleaseSave();

        await closeTask;

        Assert.False(finishedEarly, "Customer cash collection posted to shift while CloseShift was saving.");
        await Assert.ThrowsAsync<InvalidOperationException>(() => collectTask);
    }

    [Fact]
    public async Task CloseShift_Vs_ProcessRefundCashPayout_BarrierInsideSaveChanges_RejectsRefundOnClosingShift()
    {
        var barrier = new BarrierControl();
        await using var sp = BuildTestServiceProvider(barrier);

        var branchId = BranchId.New();
        var warehouseId = WarehouseId.New();
        var terminalId = TerminalId.New();
        var cashierId = Guid.NewGuid();
        var shift = Shift.Open(3, branchId, warehouseId, terminalId, new Clovent.Identity.Users.UserId(cashierId), "Cashier", 1000m);

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var order = Order.Create(OrderType.TakeAway, warehouseId);
        var line = OrderLine.Create(order.Id, ProductVariantId.New(), 1m, 300m, 0m, false);
        line.SetTaxSnapshot("Exempt", "PRA", "PK-EXEMPT", 300m, 0m, 0m, "1.3.0-AwayFromZero-v1");
        order.AddOrderLine(line.Id);
        var payment = Payment.Create(order.Id, cashMethod.Id, 300m, null);
        order.RecordPayment(payment.Id);
        order.AssignDailySalesNumber(1);
        order.Complete();

        using (var seedScope = sp.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            await db.Shifts.AddAsync(shift);
            await db.PaymentMethods.AddAsync(cashMethod);
            await db.Orders.AddAsync(order);
            await db.OrderLines.AddAsync(line);
            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();
        }

        barrier.ArmOnce();

        var closeTask = Task.Run(async () =>
        {
            using var s1 = sp.CreateScope();
            return await s1.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new CloseShiftCommand(shift.Id.Value, 1000m));
        });

        await barrier.WaitUntilSaveEnteredAsync();

        var refundTask = Task.Run(async () =>
        {
            using var s2 = sp.CreateScope();
            return await s2.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new ProcessRefundCommand(
                    order.Id.Value,
                    [new RefundItemRequest(line.Id.Value, 1m, InventoryDisposition.DamagedDiscard)],
                    "Customer return",
                    RefundSettlementMethod.CashPayout,
                    "IDEM-SHIFT-RACE-1",
                    cashierId,
                    "Cashier",
                    branchId.Value,
                    ShiftId: shift.Id.Value,
                    TerminalId: terminalId.Value));
        });

        var finishedEarly = await Task.WhenAny(refundTask, Task.Delay(150)) == refundTask;
        barrier.ReleaseSave();

        await closeTask;

        Assert.False(finishedEarly, "Cash refund executed against closing shift while CloseShift was saving.");
        await Assert.ThrowsAsync<Clovent.Restaurant.RestaurantDomainException>(() => refundTask);
    }
}
