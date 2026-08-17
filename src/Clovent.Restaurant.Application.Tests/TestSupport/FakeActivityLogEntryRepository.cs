using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.ActivityLogs;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeActivityLogEntryRepository : IActivityLogEntryRepository
{
    private readonly List<ActivityLogEntry> _entries = [];

    public void Add(ActivityLogEntry entry) => _entries.Add(entry);

    public Task AddAsync(ActivityLogEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ActivityLogEntry>> ListRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        var result = _entries.AsEnumerable().Reverse().Take(limit).ToList();
        return Task.FromResult<IReadOnlyCollection<ActivityLogEntry>>(result);
    }

    public IReadOnlyCollection<ActivityLogEntry> GetAll() => [.. _entries];
}
