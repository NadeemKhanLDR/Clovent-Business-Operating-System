using Clovent.Restaurant.Application.Concurrency;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Concurrency;

public sealed class TransactionLockManagerTests
{
    [Fact]
    public async Task ExecuteWithLocksAsync_CanceledWhileWaiting_ThrowsWithoutLeakingSemaphore()
    {
        var orderKey = TransactionLockKey.ForOrder(Guid.NewGuid());
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var holderTask = TransactionLockManager.ExecuteWithLocksAsync<int>(
            [orderKey],
            CancellationToken.None,
            async () =>
            {
                entered.SetResult(true);
                await release.Task;
                return 1;
            });

        await entered.Task;

        using var cts = new CancellationTokenSource(50);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            TransactionLockManager.ExecuteWithLocksAsync<int>(
                [orderKey],
                cts.Token,
                () => Task.FromResult(2)));

        release.SetResult(true);
        await holderTask;

        // Semaphore must still be completely released and acquirable
        var reacquired = await TransactionLockManager.ExecuteWithLocksAsync(
            [orderKey],
            CancellationToken.None,
            () => Task.FromResult(3));

        Assert.Equal(3, reacquired);
    }

    [Fact]
    public async Task ExecuteWithLocksAsync_CancellationOnSecondKey_ReleasesFirstAcquiredKeyCleanly()
    {
        var shiftKey = TransactionLockKey.ForShift(Guid.NewGuid());
        var customerKey = TransactionLockKey.ForCustomer(Guid.NewGuid());

        var customerHeld = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCustomer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Hold Customer lock externally so a multi-key waiter acquires Shift first, then blocks on Customer
        var customerHolder = TransactionLockManager.ExecuteWithLocksAsync<int>(
            [customerKey],
            CancellationToken.None,
            async () =>
            {
                customerHeld.SetResult(true);
                await releaseCustomer.Task;
                return 1;
            });

        await customerHeld.Task;

        using var cts = new CancellationTokenSource(80);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            TransactionLockManager.ExecuteWithLocksAsync<int>(
                [shiftKey, customerKey],
                cts.Token,
                () => Task.FromResult(99)));

        // Shift key must have been released when acquiring Customer failed/canceled
        var shiftAvailable = await TransactionLockManager.ExecuteWithLocksAsync(
            [shiftKey],
            new CancellationTokenSource(200).Token,
            () => Task.FromResult(42));
        Assert.Equal(42, shiftAvailable);

        releaseCustomer.SetResult(true);
        await customerHolder;
    }

    [Fact]
    public async Task ExecuteWithLocksAsync_ExceptionInsideCriticalSection_ReleasesAllLocks()
    {
        var shiftKey = TransactionLockKey.ForShift(Guid.NewGuid());
        var customerKey = TransactionLockKey.ForCustomer(Guid.NewGuid());
        var orderKey = TransactionLockKey.ForOrder(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TransactionLockManager.ExecuteWithLocksAsync<int>(
                [orderKey, customerKey, shiftKey],
                CancellationToken.None,
                () => throw new InvalidOperationException("Boom")));

        var after = await TransactionLockManager.ExecuteWithLocksAsync(
            [shiftKey, customerKey, orderKey],
            new CancellationTokenSource(200).Token,
            () => Task.FromResult(7));
        Assert.Equal(7, after);
    }

    [Fact]
    public async Task ExecuteWithLocksAsync_ConcurrentOppositeInputOrders_CompletesDeadlockFree()
    {
        var shiftKey = TransactionLockKey.ForShift(Guid.NewGuid());
        var customerKey = TransactionLockKey.ForCustomer(Guid.NewGuid());
        var orderKey = TransactionLockKey.ForOrder(Guid.NewGuid());
        var counter = 0;

        var tasks = Enumerable.Range(0, 40).Select(i => Task.Run(() =>
        {
            var keys = (i % 2 == 0)
                ? new[] { shiftKey, customerKey, orderKey }
                : new[] { orderKey, customerKey, shiftKey };

            return TransactionLockManager.ExecuteWithLocksAsync(
                keys,
                new CancellationTokenSource(5000).Token,
                async () =>
                {
                    await Task.Yield();
                    counter++;
                    return counter;
                });
        }));

        await Task.WhenAll(tasks);
        Assert.Equal(40, counter);
    }

    [Fact]
    public async Task GetSemaphore_SameKeyAcrossCycles_NeverEvictsOrReplacesInstance()
    {
        var key = TransactionLockKey.ForOrder(Guid.NewGuid());
        var first = TransactionLockManager.GetSemaphore(key);

        await TransactionLockManager.ExecuteWithLocksAsync(
            [key],
            CancellationToken.None,
            () => Task.FromResult(true));

        var second = TransactionLockManager.GetSemaphore(key);
        Assert.Same(first, second);
    }
}
