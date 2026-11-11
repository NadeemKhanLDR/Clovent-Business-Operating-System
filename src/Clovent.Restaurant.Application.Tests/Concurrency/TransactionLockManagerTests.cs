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

    [Fact]
    public async Task ExecuteWithLocksAsync_ParallelChildTasks_CannotBypassMutualExclusionViaInheritedAsyncLocal()
    {
        var key = TransactionLockKey.ForOrder(Guid.NewGuid());
        var parentEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseParent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var activeCount = 0;
        var maxObservedConcurrency = 0;

        Task child1 = Task.CompletedTask;
        Task child2 = Task.CompletedTask;

        var parentTask = TransactionLockManager.ExecuteWithLocksAsync<int>(
            [key],
            CancellationToken.None,
            async () =>
            {
                parentEntered.SetResult(true);

                child1 = Task.Run(() => TransactionLockManager.ExecuteWithLocksAsync(
                    [key],
                    CancellationToken.None,
                    async () =>
                    {
                        var current = Interlocked.Increment(ref activeCount);
                        maxObservedConcurrency = Math.Max(maxObservedConcurrency, current);
                        await Task.Delay(50);
                        Interlocked.Decrement(ref activeCount);
                        return 1;
                    }));

                child2 = Task.Run(() => TransactionLockManager.ExecuteWithLocksAsync(
                    [key],
                    CancellationToken.None,
                    async () =>
                    {
                        var current = Interlocked.Increment(ref activeCount);
                        maxObservedConcurrency = Math.Max(maxObservedConcurrency, current);
                        await Task.Delay(50);
                        Interlocked.Decrement(ref activeCount);
                        return 2;
                    }));

                await Task.Delay(80);
                Assert.False(child1.IsCompleted);
                Assert.False(child2.IsCompleted);
                Assert.Equal(0, Volatile.Read(ref activeCount));

                await releaseParent.Task;
                return 0;
            });

        await parentEntered.Task;
        await Task.Delay(120);
        releaseParent.SetResult(true);

        await parentTask;
        await Task.WhenAll(child1, child2);

        Assert.Equal(1, maxObservedConcurrency);
    }

    [Fact]
    public async Task ExecuteWithLocksAsync_DetachedChildOutlivingParent_CannotBypassAfterParentExits()
    {
        var key = TransactionLockKey.ForOrder(Guid.NewGuid());
        var startDetachedChild = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? detachedChild = null;

        await TransactionLockManager.ExecuteWithLocksAsync(
            [key],
            CancellationToken.None,
            () =>
            {
                detachedChild = Task.Run(async () =>
                {
                    await startDetachedChild.Task;
                    using var cts = new CancellationTokenSource(80);
                    return await TransactionLockManager.ExecuteWithLocksAsync(
                        [key],
                        cts.Token,
                        () => Task.FromResult(99));
                });
                return Task.FromResult(1);
            });

        var externalEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseExternal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var externalHolder = TransactionLockManager.ExecuteWithLocksAsync<int>(
            [key],
            CancellationToken.None,
            async () =>
            {
                externalEntered.SetResult(true);
                await releaseExternal.Task;
                return 0;
            });

        await externalEntered.Task;
        startDetachedChild.SetResult(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => detachedChild!);

        releaseExternal.SetResult(true);
        await externalHolder;
    }

    [Fact]
    public async Task ExecutePipelineWithLocksAsync_ParallelChildTasks_CannotBypassMutualExclusionViaInheritedAsyncLocal()
    {
        var orderId = Guid.NewGuid();
        var key = TransactionLockKey.ForOrder(orderId);
        var cmd = new { OrderId = orderId };

        var parentEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseParent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var activeInsideChildren = 0;
        var maxObservedConcurrency = 0;

        Task child1 = Task.CompletedTask;
        Task child2 = Task.CompletedTask;

        var parentTask = TransactionLockManager.ExecutePipelineWithLocksAsync<int>(
            cmd,
            [key],
            CancellationToken.None,
            async _ =>
            {
                parentEntered.SetResult(true);

                var (claimed, _) = await TransactionLockManager.TryRunUnderPipelineLeaseAsync(cmd, () => Task.FromResult(true));
                Assert.True(claimed);

                child1 = Task.Run(() => TransactionLockManager.ExecuteForRequestAsync(
                    cmd,
                    [key],
                    CancellationToken.None,
                    async () =>
                    {
                        var current = Interlocked.Increment(ref activeInsideChildren);
                        maxObservedConcurrency = Math.Max(maxObservedConcurrency, current);
                        await Task.Delay(50);
                        Interlocked.Decrement(ref activeInsideChildren);
                        return 1;
                    }));

                child2 = Task.Run(() => TransactionLockManager.ExecuteWithLocksAsync(
                    [key],
                    CancellationToken.None,
                    async () =>
                    {
                        var current = Interlocked.Increment(ref activeInsideChildren);
                        maxObservedConcurrency = Math.Max(maxObservedConcurrency, current);
                        await Task.Delay(50);
                        Interlocked.Decrement(ref activeInsideChildren);
                        return 2;
                    }));

                await Task.Delay(80);
                Assert.False(child1.IsCompleted);
                Assert.False(child2.IsCompleted);
                Assert.Equal(0, Volatile.Read(ref activeInsideChildren));

                await releaseParent.Task;
                return 0;
            });

        await parentEntered.Task;
        await Task.Delay(120);
        releaseParent.SetResult(true);

        await parentTask;
        await Task.WhenAll(child1, child2);

        Assert.Equal(1, maxObservedConcurrency);
    }

    [Fact]
    public async Task ExecutePipelineWithLocksAsync_EscapedContinuationAfterExit_CannotReuseDisposedLease()
    {
        var orderId = Guid.NewGuid();
        var key = TransactionLockKey.ForOrder(orderId);
        var cmd = new { OrderId = orderId };
        Func<Task<int>>? escapedInvocation = null;

        await TransactionLockManager.ExecutePipelineWithLocksAsync<int>(
            cmd,
            [key],
            CancellationToken.None,
            _ =>
            {
                escapedInvocation = () => TransactionLockManager.ExecuteForRequestAsync(
                    cmd,
                    [key],
                    new CancellationTokenSource(80).Token,
                    () => Task.FromResult(99));
                return Task.FromResult(1);
            });

        var blockerEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBlocker = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var blocker = TransactionLockManager.ExecuteWithLocksAsync<int>(
            [key],
            CancellationToken.None,
            async () =>
            {
                blockerEntered.SetResult(true);
                await releaseBlocker.Task;
                return 0;
            });

        await blockerEntered.Task;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => escapedInvocation!());

        releaseBlocker.SetResult(true);
        await blocker;
    }

    [Fact]
    public async Task ExecuteNestedRequestAsync_SequentialNestedOperation_ReusesHeldKeysAndAcquiresStrictlyHigherCanonicalKeys()
    {
        var shiftKey = TransactionLockKey.ForShift(Guid.NewGuid());
        var customerKey = TransactionLockKey.ForCustomer(Guid.NewGuid());
        var orderKey = TransactionLockKey.ForOrder(Guid.NewGuid());
        var parentCmd = new { Scope = "Outer" };
        var nestedCmd = new { Scope = "Inner" };
        var acquiredSnapshot = Array.Empty<TransactionLockKey>();

        await TransactionLockManager.ExecutePipelineWithLocksAsync<int>(
            parentCmd,
            [shiftKey],
            CancellationToken.None,
            async _ =>
            {
                var (claimed, _) = await TransactionLockManager.TryRunUnderPipelineLeaseAsync(parentCmd, () => Task.FromResult(true));
                Assert.True(claimed);

                // Shift is already held; Customer and Order are strictly higher in canonical order (Shift < Customer < Order).
                await TransactionLockManager.ExecuteNestedRequestAsync(
                    nestedCmd,
                    () => TransactionLockManager.ExecutePipelineWithLocksAsync(
                        nestedCmd,
                        [orderKey, shiftKey, customerKey],
                        CancellationToken.None,
                        async newlyAcquired =>
                        {
                            acquiredSnapshot = [.. newlyAcquired];
                            var (innerClaimed, innerResult) = await TransactionLockManager.TryRunUnderPipelineLeaseAsync(
                                nestedCmd,
                                () => Task.FromResult(77));
                            Assert.True(innerClaimed);
                            return innerResult;
                        }));

                // Strictly higher nested locks must be released when nested operation exits, while parent Shift remains held.
                var orderFree = await TransactionLockManager.ExecuteWithLocksAsync(
                    [orderKey],
                    new CancellationTokenSource(100).Token,
                    () => Task.FromResult(true));
                Assert.True(orderFree);

                return 0;
            });

        Assert.Equal([customerKey, orderKey], acquiredSnapshot);
    }

    [Fact]
    public async Task ExecuteNestedRequestAsync_LowerCanonicalKeyNotAlreadyHeld_ThrowsCanonicalHierarchyViolation()
    {
        var shiftKey = TransactionLockKey.ForShift(Guid.NewGuid());
        var customerKey = TransactionLockKey.ForCustomer(Guid.NewGuid());
        var orderKey = TransactionLockKey.ForOrder(Guid.NewGuid());
        var parentCmd = new { Scope = "OuterOrderOnly" };
        var nestedCmd = new { Scope = "InnerCustomerOutOfOrder" };

        await TransactionLockManager.ExecutePipelineWithLocksAsync<int>(
            parentCmd,
            [orderKey],
            CancellationToken.None,
            async _ =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    TransactionLockManager.ExecuteNestedRequestAsync(
                        nestedCmd,
                        () => TransactionLockManager.ExecuteForRequestAsync(
                            nestedCmd,
                            [customerKey, shiftKey, orderKey],
                            CancellationToken.None,
                            () => Task.FromResult(1))));
                return 0;
            });
    }

    [Fact]
    public async Task ExecuteNestedRequestAsync_ConcurrentSiblingNestingOrDetachedPostExit_EnforcesSafety()
    {
        var key = TransactionLockKey.ForCustomer(Guid.NewGuid());
        var parentCmd = new { Scope = "Outer" };
        var nestedEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseNested = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<Task<int>>? postExitNested = null;

        await TransactionLockManager.ExecutePipelineWithLocksAsync<int>(
            parentCmd,
            [key],
            CancellationToken.None,
            async _ =>
            {
                var firstCmd = new { Name = "First" };
                var secondCmd = new { Name = "Second" };

                var firstTask = TransactionLockManager.ExecuteNestedRequestAsync(
                    firstCmd,
                    async () =>
                    {
                        nestedEntered.SetResult(true);
                        await releaseNested.Task;
                        return 1;
                    });

                await nestedEntered.Task;
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    TransactionLockManager.ExecuteNestedRequestAsync(secondCmd, () => Task.FromResult(2)));

                releaseNested.SetResult(true);
                Assert.Equal(1, await firstTask);

                var lateCmd = new { Name = "Late" };
                postExitNested = () => TransactionLockManager.ExecuteNestedRequestAsync(
                    lateCmd,
                    () => TransactionLockManager.ExecuteForRequestAsync(
                        lateCmd,
                        [key],
                        new CancellationTokenSource(80).Token,
                        () => Task.FromResult(3)));

                return 0;
            });

        var blockerEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBlocker = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var blocker = TransactionLockManager.ExecuteWithLocksAsync<int>(
            [key],
            CancellationToken.None,
            async () =>
            {
                blockerEntered.SetResult(true);
                await releaseBlocker.Task;
                return 0;
            });

        await blockerEntered.Task;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => postExitNested!());

        releaseBlocker.SetResult(true);
        await blocker;
    }
}
