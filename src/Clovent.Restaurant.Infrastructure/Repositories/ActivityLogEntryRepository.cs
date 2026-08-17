using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IActivityLogEntryRepository"/>.</summary>
public sealed class ActivityLogEntryRepository(RestaurantDbContext dbContext) : IActivityLogEntryRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(ActivityLogEntry entry, CancellationToken cancellationToken = default) =>
        await dbContext.ActivityLogEntries.AddAsync(entry, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ActivityLogEntry>> ListRecentAsync(int limit, CancellationToken cancellationToken = default) =>
        await dbContext.ActivityLogEntries
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
