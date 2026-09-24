using Clovent.Restaurant.SmartRecommendations;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeRecommendationRuleRepository : IRecommendationRuleRepository
{
    private readonly Dictionary<RecommendationRuleId, RecommendationRule> _rules = [];

    public void Add(RecommendationRule rule) => _rules[rule.Id] = rule;

    public Task<RecommendationRule?> GetByIdAsync(RecommendationRuleId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_rules.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<RecommendationRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<RecommendationRule>>([.. _rules.Values]);

    public Task<IReadOnlyCollection<RecommendationRule>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<RecommendationRule>>([.. _rules.Values.Where(r => r.IsActive)]);

    public Task AddAsync(RecommendationRule rule, CancellationToken cancellationToken = default)
    {
        _rules[rule.Id] = rule;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RecommendationRule rule, CancellationToken cancellationToken = default)
    {
        _rules[rule.Id] = rule;
        return Task.CompletedTask;
    }
}
