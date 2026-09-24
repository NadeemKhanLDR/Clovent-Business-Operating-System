namespace Clovent.Restaurant.SmartRecommendations;

/// <summary>Persistence contract for <see cref="RecommendationRule"/> aggregates.</summary>
public interface IRecommendationRuleRepository
{
    /// <summary>Retrieves a rule by identity, or <see langword="null"/> if none exists.</summary>
    Task<RecommendationRule?> GetByIdAsync(RecommendationRuleId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every rule, regardless of status.</summary>
    Task<IReadOnlyCollection<RecommendationRule>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves every currently-active rule.</summary>
    Task<IReadOnlyCollection<RecommendationRule>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created rule.</summary>
    Task AddAsync(RecommendationRule rule, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing rule.</summary>
    Task UpdateAsync(RecommendationRule rule, CancellationToken cancellationToken = default);
}
