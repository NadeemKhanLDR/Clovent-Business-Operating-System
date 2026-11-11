using Clovent.Restaurant.Application.Concurrency;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

public sealed class SqlTransactionLockTests
{
    [Fact]
    public async Task FailureAndCanceledWait_ReleaseSessionLocksWithoutExecutingWaitingBody()
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(SqlTestDatabaseGuard.ConnectionString($"CBOS_Recovery_{Guid.NewGuid():N}")).Options;
        await using var first = new RestaurantDbContext(options);
        await using var second = new RestaurantDbContext(options);
        var key = TransactionLockKey.ForOrder(Guid.NewGuid());
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int>? holder = null;
        try
        {
            await first.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(first);
            var firstLocks = new SqlTransactionLockProvider(first);
            var secondLocks = new SqlTransactionLockProvider(second);
            holder = firstLocks.ExecuteAsync<int>([key], CancellationToken.None, async () =>
            {
                entered.SetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(15));
                throw new InvalidOperationException("injected handler failure");
            });
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var executed = false;
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => secondLocks.ExecuteAsync([key], cancellation.Token,
                () => { executed = true; return Task.FromResult(1); }));
            Assert.False(executed);
            release.TrySetResult();
            await Assert.ThrowsAsync<InvalidOperationException>(() => holder);
            Assert.Equal(42, await secondLocks.ExecuteAsync([key], CancellationToken.None, () => Task.FromResult(42)));
        }
        finally
        {
            release.TrySetResult();
            if (holder is not null) { try { await holder; } catch (InvalidOperationException) { } }
            await SqlTestDatabaseGuard.DeleteAndVerifyAsync(first);
        }
    }
}