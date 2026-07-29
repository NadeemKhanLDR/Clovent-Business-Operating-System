using Clovent.MasterData.TimeZones;

namespace Clovent.MasterData.Application.Tests.TestSupport;

internal sealed class FakeTimeZoneRepository : ITimeZoneRepository
{
    private readonly Dictionary<TimeZoneEntryId, TimeZoneEntry> _entries = [];

    public void Add(TimeZoneEntry entry) => _entries[entry.Id] = entry;

    public Task<TimeZoneEntry?> GetByIdAsync(TimeZoneEntryId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_entries.GetValueOrDefault(id));

    public Task<TimeZoneEntry?> GetByIanaIdAsync(IanaId ianaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_entries.Values.FirstOrDefault(e => e.IanaId == ianaId));

    public Task<IReadOnlyCollection<TimeZoneEntry>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<TimeZoneEntry>>([.. _entries.Values]);

    public Task AddAsync(TimeZoneEntry timeZoneEntry, CancellationToken cancellationToken = default)
    {
        _entries[timeZoneEntry.Id] = timeZoneEntry;
        return Task.CompletedTask;
    }
}
