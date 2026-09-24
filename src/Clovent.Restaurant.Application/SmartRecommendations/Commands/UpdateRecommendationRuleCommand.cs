using Clovent.Catalog.Variants;
using Clovent.Restaurant.Application;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.SmartRecommendations;
using MediatR;

namespace Clovent.Restaurant.Application.SmartRecommendations.Commands;

/// <summary>Updates an existing recommendation rule's configuration.</summary>
public sealed record UpdateRecommendationRuleCommand(
    Guid RuleId,
    Guid? ProductId,
    Guid RecommendedVariantId,
    int Priority,
    TimeSpan? StartTime = null,
    TimeSpan? EndTime = null,
    int? DaysOfWeek = null,
    string? Notes = null) : IRequest<RecommendationRuleDto>;

/// <summary>Handles <see cref="UpdateRecommendationRuleCommand"/>.</summary>
public sealed class UpdateRecommendationRuleCommandHandler(IRecommendationRuleRepository repository)
    : IRequestHandler<UpdateRecommendationRuleCommand, RecommendationRuleDto>
{
    /// <inheritdoc/>
    public async Task<RecommendationRuleDto> Handle(UpdateRecommendationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(new RecommendationRuleId(request.RuleId), cancellationToken)
            ?? throw new NotFoundException(nameof(RecommendationRule), request.RuleId);

        rule.Update(
            request.ProductId,
            new ProductVariantId(request.RecommendedVariantId),
            request.Priority,
            request.StartTime,
            request.EndTime,
            request.DaysOfWeek,
            request.Notes);

        await repository.UpdateAsync(rule, cancellationToken);
        return RecommendationRuleDto.FromDomain(rule);
    }
}
