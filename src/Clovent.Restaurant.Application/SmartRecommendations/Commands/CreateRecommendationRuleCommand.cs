using Clovent.Catalog.Variants;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.SmartRecommendations;
using MediatR;

namespace Clovent.Restaurant.Application.SmartRecommendations.Commands;

/// <summary>Creates a new recommendation rule from the Back Office.</summary>
public sealed record CreateRecommendationRuleCommand(
    Guid? ProductId,
    Guid RecommendedVariantId,
    int Priority,
    TimeSpan? StartTime = null,
    TimeSpan? EndTime = null,
    int? DaysOfWeek = null,
    string? Notes = null) : IRequest<RecommendationRuleDto>;

/// <summary>Handles <see cref="CreateRecommendationRuleCommand"/>.</summary>
public sealed class CreateRecommendationRuleCommandHandler(IRecommendationRuleRepository repository)
    : IRequestHandler<CreateRecommendationRuleCommand, RecommendationRuleDto>
{
    /// <inheritdoc/>
    public async Task<RecommendationRuleDto> Handle(CreateRecommendationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = RecommendationRule.Create(
            request.ProductId,
            new ProductVariantId(request.RecommendedVariantId),
            request.Priority,
            request.StartTime,
            request.EndTime,
            request.DaysOfWeek,
            request.Notes);

        await repository.AddAsync(rule, cancellationToken);
        return RecommendationRuleDto.FromDomain(rule);
    }
}
