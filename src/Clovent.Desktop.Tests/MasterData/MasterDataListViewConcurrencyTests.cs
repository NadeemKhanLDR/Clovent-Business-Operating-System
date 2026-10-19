using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.MasterData;
using Xunit;

namespace Clovent.Desktop.Tests.MasterData;

public sealed class MasterDataListViewConcurrencyTests
{
    private sealed record TestRow(Guid Id, string Name);

    [Fact]
    public async Task RefreshAsync_ConcurrentCalls_NeverExecuteLoadItemsConcurrently()
    {
        using var listView = new MasterDataListView<TestRow>(
        [
            new MasterDataColumn("Name", "Name", 150)
        ]);

        var inFlight = 0;
        var maxInFlight = 0;
        var executionCount = 0;

        listView.LoadItemsAsync = async (ct) =>
        {
            var current = Interlocked.Increment(ref inFlight);
            lock (listView)
            {
                if (current > maxInFlight)
                {
                    maxInFlight = current;
                }
            }

            Interlocked.Increment(ref executionCount);
            await Task.Delay(20, ct);

            Interlocked.Decrement(ref inFlight);
            return new List<TestRow> { new(Guid.NewGuid(), "Item 1") };
        };

        // Fire 5 concurrent refresh tasks
        var tasks = new List<Task>();
        for (var i = 0; i < 5; i++)
        {
            tasks.Add(listView.RefreshAsync());
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, maxInFlight);
        Assert.True(executionCount <= 2, $"Expected at most 2 executions (initial + coalesced follow-up), but got {executionCount}");
    }

    [Fact]
    public async Task RefreshAsync_DisposalDuringInFlightRefresh_DrainsSafelyWithoutObjectDisposedException()
    {
        var listView = new MasterDataListView<TestRow>(
        [
            new MasterDataColumn("Name", "Name", 150)
        ]);

        var startedTcs = new TaskCompletionSource<bool>();

        listView.LoadItemsAsync = async (ct) =>
        {
            startedTcs.TrySetResult(true);
            try
            {
                await Task.Delay(500, ct);
            }
            catch (OperationCanceledException)
            {
                // Expected when disposal cancels _disposeCts
            }
            return new List<TestRow> { new(Guid.NewGuid(), "Item 1") };
        };

        var refreshTask = listView.RefreshAsync();
        await startedTcs.Task;

        // Dispose view while refresh is in flight
        listView.Dispose();

        // Should complete without throwing unhandled ObjectDisposedException
        await refreshTask;
    }

    [Fact]
    public async Task RefreshAsync_FailedOrCanceledRefresh_ReleasesGateAndAllowsSubsequentRefresh()
    {
        using var listView = new MasterDataListView<TestRow>(
        [
            new MasterDataColumn("Name", "Name", 150)
        ]);

        var shouldFail = true;
        var executionCount = 0;

        listView.LoadItemsAsync = (ct) =>
        {
            Interlocked.Increment(ref executionCount);
            if (shouldFail)
            {
                throw new InvalidOperationException("Simulated transient query failure");
            }
            return Task.FromResult<IReadOnlyList<TestRow>>(new List<TestRow> { new(Guid.NewGuid(), "Success Item") });
        };

        // First refresh fails
        await Assert.ThrowsAsync<InvalidOperationException>(() => listView.RefreshAsync());
        Assert.Equal(1, executionCount);

        // Subsequent refresh should acquire the released gate and succeed
        shouldFail = false;
        await listView.RefreshAsync();
        Assert.Equal(2, executionCount);
    }

    [Fact]
    public async Task RefreshAsync_RapidSelectionChanges_CoalescesAndDisplaysLatestSelection()
    {
        using var listView = new MasterDataListView<TestRow>(
        [
            new MasterDataColumn("Name", "Name", 150)
        ]);

        var currentSelectedCategory = "Category A";
        var queriedCategories = new List<string>();

        listView.LoadItemsAsync = async (ct) =>
        {
            var categoryToQuery = currentSelectedCategory;
            await Task.Delay(30, ct);
            lock (queriedCategories)
            {
                queriedCategories.Add(categoryToQuery);
            }
            return new List<TestRow> { new(Guid.NewGuid(), $"Item for {categoryToQuery}") };
        };

        // Initial refresh starts querying Category A
        var task1 = listView.RefreshAsync();

        // User rapidly switches to Category B then Category C while task1 is running
        currentSelectedCategory = "Category B";
        var task2 = listView.RefreshAsync();

        currentSelectedCategory = "Category C";
        var task3 = listView.RefreshAsync();

        await Task.WhenAll(task1, task2, task3);

        // First query ran Category A, and coalesced follow-up query picked up latest (Category C)
        Assert.Equal(2, queriedCategories.Count);
        Assert.Equal("Category A", queriedCategories[0]);
        Assert.Equal("Category C", queriedCategories[1]);
    }
}
