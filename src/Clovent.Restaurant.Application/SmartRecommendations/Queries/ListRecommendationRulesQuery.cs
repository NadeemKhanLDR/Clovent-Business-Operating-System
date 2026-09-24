using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.SmartRecommendations;
using MediatR;

namespace Clovent.Restaurant.Application.SmartRecommendations.Queries;

/// <summary>Retrieves every recommendation rule (active and inactive), ordered by priority - the Back Office management list.</summary>
public sealed record ListRecommendationRulesQuery : IRequest<IReadOnlyCollection<RecommendationRuleDto>>;

/// <summary>Handles <see cref="ListRecommendationRulesQuery"/>.</summary>
public sealed class ListRecommendationRulesQueryHandler(IRecommendationRuleRepository repository)
    : IRequestHandler<ListRecommendationRulesQuery, IReadOnlyCollection<RecommendationRuleDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<RecommendationRuleDto>> Handle(ListRecommendationRulesQuery request, CancellationToken cancellationToken)
    {
        var rules = await repository.GetAllAsync(cancellationToken);
        return [.. rules.OrderBy(r => r.Priority).ThenBy(r => r.Id.Value).Select(RecommendationRuleDto.FromDomain)];
    }
}
