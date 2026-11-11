using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

public sealed class SqlPaymentRecoveryTests
{
    [Fact]
    public async Task CreditPayment_DatabaseFailureRollsBackBalanceLedgerAndPayment_ThenRetryCommitsOnce()
    {
        var database = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(SqlTestDatabaseGuard.ConnectionString(database)).Options;
        await using var db = new RestaurantDbContext(options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(db);
            var (order, method) = await FailedCommandRecoveryTests.SeedAsync(db);
            var customer = Clovent.Restaurant.Customers.Customer.Create(
                Clovent.MasterData.Shared.ValueObjects.EntityCode.Create("RECOVERY-CREDIT"),
                "Synthetic customer", "03000000000", "Synthetic address", null, 0m, 1000m, null);
            db.Customers.Add(customer);
            var currentOrder = await db.Orders.SingleAsync();
            currentOrder.SetCustomer(customer.Id);
            var currentMethod = await db.PaymentMethods.SingleAsync();
            currentMethod.Rename(Clovent.Restaurant.PaymentMethods.ValueObjects.PaymentMethodName.Create("On Account"));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            using var provider = FailedCommandRecoveryTests.Pipeline(db, new());
            var mediator = provider.GetRequiredService<IMediator>();
            var command = new RecordPaymentCommand(order.Id.Value, method.Id.Value, 100m, IdempotencyKey: "credit-retry");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [Restaurant].[Payments] ADD CONSTRAINT CK_InjectedFailure CHECK ([Amount] < 0);");
            var failure = await Assert.ThrowsAsync<DbUpdateException>(() => mediator.Send(command));
            Assert.Contains("CK_InjectedFailure", failure.ToString());
            Assert.Empty(db.ChangeTracker.Entries());
            await using (var fresh = new RestaurantDbContext(options))
            {
                Assert.Equal(0m, (await fresh.Customers.SingleAsync()).OutstandingBalance);
                Assert.Equal(0, await fresh.Payments.CountAsync());
                Assert.Equal(0, await fresh.CustomerLedgerEntries.CountAsync());
                Assert.Equal(0, await fresh.CustomerPaymentAllocations.CountAsync());
            }
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [Restaurant].[Payments] DROP CONSTRAINT CK_InjectedFailure;");
            var saved = await mediator.Send(command);
            Assert.Equal(saved.PaymentId, (await mediator.Send(command)).PaymentId);
            await using var verify = new RestaurantDbContext(options);
            Assert.Equal(100m, (await verify.Customers.SingleAsync()).OutstandingBalance);
            Assert.Single(await verify.CustomerLedgerEntries.ToListAsync());
            Assert.Single(await verify.Payments.ToListAsync());
        }
        finally { await SqlTestDatabaseGuard.DeleteAndVerifyAsync(db); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InterruptedCommit_RetrySameKeyHasExactlyOnePayment(bool afterCommit)
    {
        var database = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var interruption = new CommitAcknowledgementBarrier();
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(SqlTestDatabaseGuard.ConnectionString(database))
            .AddInterceptors(interruption).Options;
        await using var db = new RestaurantDbContext(options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(db);
            var (order, method) = await FailedCommandRecoveryTests.SeedAsync(db);
            using var provider = FailedCommandRecoveryTests.Pipeline(db, new());
            var mediator = provider.GetRequiredService<IMediator>();
            var command = new RecordPaymentCommand(order.Id.Value, method.Id.Value, 100m, IdempotencyKey: "uncertain-result");
            interruption.AfterCommit = afterCommit;
            interruption.Armed = true;
            var attempt = mediator.Send(command);
            await interruption.Reached.Task.WaitAsync(TimeSpan.FromSeconds(20));
            interruption.Release.SetResult();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attempt);
            Assert.Empty(db.ChangeTracker.Entries());

            await using (var fresh = new RestaurantDbContext(options))
                Assert.Equal(afterCommit ? 1 : 0, await fresh.Payments.CountAsync());

            var recovered = await mediator.Send(command);
            Assert.Equal(recovered.PaymentId, (await mediator.Send(command)).PaymentId);
            await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(command with { IdempotencyKey = "new-key" }));
            await using var verify = new RestaurantDbContext(options);
            Assert.Equal(100m, Assert.Single(await verify.Payments.ToListAsync()).Amount);
            Assert.Equal(0, await verify.CustomerLedgerEntries.CountAsync());
        }
        finally
        {
            // Only the GUID-named database created by this test is removed.
            await SqlTestDatabaseGuard.DeleteAndVerifyAsync(db);
        }
    }

    private sealed class CommitAcknowledgementBarrier : SaveChangesInterceptor
    {
        public bool Armed;
        public bool AfterCommit;
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private async Task InterruptAsync()
        {
            Armed = false;
            Reached.SetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(20));
            throw new OperationCanceledException("Injected cancellation at persistence/acknowledgement boundary");
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Armed && !AfterCommit) await InterruptAsync();
            return result;
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (Armed && AfterCommit) await InterruptAsync();
            return result;
        }
    }
}
