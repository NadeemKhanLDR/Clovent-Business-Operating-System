namespace Clovent.Restaurant.ActivityLogs;

/// <summary>Persistence contract for <see cref="ActivityLogEntry"/> aggregates.</summary>
public interface IActivityLogEntryRepository
{
    /// <summary>Adds a newly-recorded activity log entry.</summary>
    Task AddAsync(ActivityLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the most recent entries, newest first, capped at <paramref name="limit"/>.</summary>
    Task<IReadOnlyCollection<ActivityLogEntry>> ListRecentAsync(int limit, CancellationToken cancellationToken = default);
}
