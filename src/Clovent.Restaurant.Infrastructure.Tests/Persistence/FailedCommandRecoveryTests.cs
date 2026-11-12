using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

// Real handler -> production MediatR commit behavior -> EF/SQLite relational boundary.
// These tests do not qualify SQL Server locking or commit acknowledgement behavior.
public sealed class FailedCommandRecoveryTests : SqliteTestBase
{
    [Fact]
    public async Task VoidOpenOrder_WithReadOnlyPaymentLists_PersistsVoidAndKeepsPaymentHistory()
    {
        await using var db = CreateContext();
        var (order, method) = await SeedAsync(db);
        using var provider = Pipeline(db, new());
        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Send(new RecordPaymentCommand(order.Id.Value, method.Id.Value, 40m, IdempotencyKey: "void-merge"));
        await mediator.Send(new Clovent.Restaurant.Application.Orders.Commands.VoidOrderCommand(order.Id.Value, "Synthetic erroneous payment"));
        await using var verify = CreateContext();
        Assert.Equal(OrderStatus.Voided, (await verify.Orders.SingleAsync()).Status);
        var payment = Assert.Single(await verify.Payments.ToListAsync());
        Assert.True(payment.IsVoided);
        Assert.Equal(40m, payment.Amount);
    }
    [Fact]
    public async Task OldScope_AfterAnotherScopeVoidsPayment_UsesAuthoritativeBalance()
    {
        await using var db = CreateContext();
        var (order, method) = await SeedAsync(db);
        using var provider = Pipeline(db, new());
        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Send(new RecordPaymentCommand(order.Id.Value, method.Id.Value, 40m, IdempotencyKey: "voided"));
        await using (var other = CreateContext())
        {
            (await other.Payments.SingleAsync()).Void();
            await other.SaveChangesAsync();
        }
        await mediator.Send(new RecordPaymentCommand(order.Id.Value, method.Id.Value, 100m, IdempotencyKey: "replacement"));
        await using var verify = CreateContext();
        var payments = await verify.Payments.ToListAsync();
        Assert.Equal(2, payments.Count);
        Assert.Equal(40m, Assert.Single(payments, p => p.IsVoided).Amount);
        Assert.Equal(100m, Assert.Single(payments, p => !p.IsVoided).Amount);
    }

    [Fact]
    public async Task OldScope_AfterAnotherScopeClosesShift_RejectsNewPayment()
    {
        await using var db = CreateContext();
        var (order, method) = await SeedAsync(db);
        var shift = Clovent.Restaurant.Shifts.Shift.Open(1,
            new Clovent.Identity.Branches.BranchId(Guid.NewGuid()), order.WarehouseId,
            new Clovent.MasterData.Terminals.TerminalId(Guid.NewGuid()),
            new Clovent.Identity.Users.UserId(Guid.NewGuid()), "Synthetic cashier", 0m);
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();
        using var provider = Pipeline(db, new());
        await using (var other = CreateContext())
        {
            var current = await other.Shifts.SingleAsync();
            current.Close(0m, 0m, null);
            await other.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<RestaurantDomainException>(() => provider.GetRequiredService<IMediator>().Send(
            new RecordPaymentCommand(order.Id.Value, method.Id.Value, 100m, ShiftId: shift.Id.Value, IdempotencyKey: "closed-shift")));
        await using var verify = CreateContext();
        Assert.Equal(0, await verify.Payments.CountAsync());
        Assert.Equal(Clovent.Restaurant.Shifts.ShiftStatus.Closed, (await verify.Shifts.SingleAsync()).Status);
    }

    [Fact]
    public async Task OldScope_AfterAnotherScopeCompletesPaidOrder_RejectsEdits()
    {
        await using var db = CreateContext();
        var (order, method) = await SeedAsync(db);
        using var provider = Pipeline(db, new());
        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Send(new RecordPaymentCommand(order.Id.Value, method.Id.Value, 100m, IdempotencyKey: "first"));
        await using (var other = CreateContext())
        {
            var current = await other.Orders.SingleAsync();
            current.Complete();
            await other.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<RestaurantDomainException>(() => mediator.Send(
            new Clovent.Restaurant.Application.Orders.Commands.SetOrderCustomerCommand(order.Id.Value, null)));
        await using var verify = CreateContext();
        Assert.Single(await verify.Payments.ToListAsync());
        Assert.Equal(OrderStatus.Completed, (await verify.Orders.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedAttempt_CannotLeakIntoLaterCommandInSameScope(bool cancel)
    {
        await using var db = CreateContext();
        var (order, method) = await SeedAsync(db);
        using var provider = Pipeline(db, new FailureGate { Fail = true, Cancel = cancel });
        var mediator = provider.GetRequiredService<IMediator>();
        var gate = provider.GetRequiredService<FailureGate>();
        var failed = new RecordPaymentCommand(order.Id.Value, method.Id.Value, 40m, IdempotencyKey: "failed");

        if (cancel)
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mediator.Send(failed));
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(failed));

        await using (var fresh = CreateContext()) Assert.Equal(0, await fresh.Payments.CountAsync());
        Assert.Empty(db.ChangeTracker.Entries());
        gate.Fail = false;
        await mediator.Send(failed with { Amount = 60m, IdempotencyKey = "next" });
        await using var verify = CreateContext();
        var payment = Assert.Single(await verify.Payments.ToListAsync());
        Assert.Equal(60m, payment.Amount);
        Assert.Equal("next", payment.IdempotencyKey);
        Assert.Equal(0, await verify.CustomerLedgerEntries.CountAsync());
    }

    [Fact]
    public async Task PersistenceFailure_DoesNotReturnStagedPaymentAsSuccessfulRetry()
    {
        await using var db = CreateContext();
        var (order, method) = await SeedAsync(db);
        using var provider = Pipeline(db, new FailureGate());
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new RecordPaymentCommand(order.Id.Value, method.Id.Value, 100m, IdempotencyKey: "retry");
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_payment BEFORE INSERT ON Payments BEGIN SELECT RAISE(ABORT, 'injected persistence failure'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => mediator.Send(command));
        Assert.Empty(db.ChangeTracker.Entries());
        await using (var fresh = CreateContext()) Assert.Equal(0, await fresh.Payments.CountAsync());
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_payment;");
        var result = await mediator.Send(command);
        var retry = await mediator.Send(command);
        Assert.Equal(result.PaymentId, retry.PaymentId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(command with { Amount = 99m }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(command with { IdempotencyKey = "different" }));
        await using var verify = CreateContext();
        Assert.Equal(100m, Assert.Single(await verify.Payments.ToListAsync()).Amount);
    }

    internal static async Task<(Order, PaymentMethod)> SeedAsync(RestaurantDbContext db)
    {
        var order = Order.Create(OrderType.TakeAway, new WarehouseId(Guid.NewGuid()), null, OrderNumber.Generate(DateTimeOffset.UtcNow));
        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        db.AddRange(order, method, OrderLine.Create(order.Id, new ProductVariantId(Guid.NewGuid()), 1m, 100m, 0m, false));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return (order, method);
    }

    internal static ServiceProvider Pipeline(RestaurantDbContext db, FailureGate gate)
    {
        var services = new ServiceCollection();
        services.AddSingleton<Clovent.Restaurant.Application.Security.IFinancialCompanyScope, Clovent.Restaurant.Infrastructure.Tests.TestSupport.SyntheticFinancialScope>();
        services.AddSingleton<Clovent.Restaurant.Application.IUnitOfWork>(new UnitOfWork(db));
        services.AddSingleton(gate);
        services.AddSingleton<IOrderRepository>(new OrderRepository(db));
        services.AddSingleton<Clovent.Restaurant.Tables.ITableRepository>(new TableRepository(db));
        services.AddSingleton<IPaymentRepository>(new PaymentRepository(db));
        services.AddSingleton<IPaymentMethodRepository>(new PaymentMethodRepository(db));
        services.AddSingleton<IOrderLineRepository>(new OrderLineRepository(db));
        services.AddSingleton<IDiscountRepository>(new DiscountRepository(db));
        services.AddSingleton<IServiceChargeRepository>(new ServiceChargeRepository(db));
        services.AddSingleton<ICustomerRepository>(new CustomerRepository(db));
        services.AddSingleton<ICustomerLedgerEntryRepository>(new CustomerLedgerEntryRepository(db));
        services.AddSingleton<ICustomerPaymentAllocationRepository>(new CustomerPaymentAllocationRepository(db));
        services.AddSingleton<Clovent.Restaurant.Shifts.IShiftRepository>(new ShiftRepository(db));
        services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<RecordPaymentCommand>());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        services.AddTransient<IPipelineBehavior<RecordPaymentCommand, PaymentDto>, FailureAfterHandler>();
        return services.BuildServiceProvider();
    }

    internal sealed class FailureGate { public bool Fail; public bool Cancel; }
    private sealed class FailureAfterHandler(FailureGate gate) : IPipelineBehavior<RecordPaymentCommand, PaymentDto>
    {
        public async Task<PaymentDto> Handle(RecordPaymentCommand request, RequestHandlerDelegate<PaymentDto> next, CancellationToken cancellationToken)
        {
            var result = await next();
            if (gate.Fail)
            {
                if (gate.Cancel) throw new OperationCanceledException("injected before persistence");
                throw new InvalidOperationException("injected after handler staged payment");
            }
            return result;
        }
    }
}
