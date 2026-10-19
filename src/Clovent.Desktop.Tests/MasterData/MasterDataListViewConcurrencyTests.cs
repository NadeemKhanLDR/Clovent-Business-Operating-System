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
}
