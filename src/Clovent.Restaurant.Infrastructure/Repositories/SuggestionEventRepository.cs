using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.SmartRecommendations;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ISuggestionEventRepository"/>.</summary>
public sealed class SuggestionEventRepository(RestaurantDbContext dbContext) : ISuggestionEventRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(SuggestionEvent suggestionEvent, CancellationToken cancellationToken = default) =>
        await dbContext.SuggestionEvents.AddAsync(suggestionEvent, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<SuggestionEvent>> GetRangeAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
        await dbContext.SuggestionEvents
            .AsNoTracking()
            .Where(e => e.OccurredAtUtc >= fromUtc && e.OccurredAtUtc <= toUtc)
            .ToListAsync(cancellationToken);
}
