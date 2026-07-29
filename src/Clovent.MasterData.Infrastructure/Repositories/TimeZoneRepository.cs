using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.MasterData.TimeZones;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ITimeZoneRepository"/>.</summary>
public sealed class TimeZoneRepository(MasterDataDbContext dbContext) : ITimeZoneRepository
{
    /// <inheritdoc/>
    public Task<TimeZoneEntry?> GetByIdAsync(TimeZoneEntryId id, CancellationToken cancellationToken = default) =>
        dbContext.TimeZoneEntries.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<TimeZoneEntry?> GetByIanaIdAsync(IanaId ianaId, CancellationToken cancellationToken = default) =>
        dbContext.TimeZoneEntries.FirstOrDefaultAsync(t => t.IanaId == ianaId, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<TimeZoneEntry>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.TimeZoneEntries.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(TimeZoneEntry timeZoneEntry, CancellationToken cancellationToken = default) =>
        await dbContext.TimeZoneEntries.AddAsync(timeZoneEntry, cancellationToken);
}
