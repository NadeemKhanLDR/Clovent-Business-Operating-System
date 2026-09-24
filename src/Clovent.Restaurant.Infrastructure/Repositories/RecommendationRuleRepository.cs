using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.SmartRecommendations;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IRecommendationRuleRepository"/>.</summary>
public sealed class RecommendationRuleRepository(RestaurantDbContext dbContext) : IRecommendationRuleRepository
{
    /// <inheritdoc/>
    public async Task<RecommendationRule?> GetByIdAsync(RecommendationRuleId id, CancellationToken cancellationToken = default) =>
        await dbContext.RecommendationRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<RecommendationRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RecommendationRules.AsNoTracking().OrderBy(r => r.Priority).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<RecommendationRule>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RecommendationRules.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Priority).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(RecommendationRule rule, CancellationToken cancellationToken = default) =>
        await dbContext.RecommendationRules.AddAsync(rule, cancellationToken);

    /// <inheritdoc/>
    public Task UpdateAsync(RecommendationRule rule, CancellationToken cancellationToken = default)
    {
        if (dbContext.Entry(rule).State == EntityState.Detached)
        {
            dbContext.RecommendationRules.Attach(rule).State = EntityState.Modified;
        }

        return Task.CompletedTask;
    }
}
