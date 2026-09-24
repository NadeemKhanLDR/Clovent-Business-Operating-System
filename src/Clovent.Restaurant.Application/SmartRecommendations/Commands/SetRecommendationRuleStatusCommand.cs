using Clovent.Restaurant.Application;
using Clovent.Restaurant.SmartRecommendations;
using MediatR;

namespace Clovent.Restaurant.Application.SmartRecommendations.Commands;

/// <summary>Activates or deactivates a recommendation rule - the Back Office "soft delete".</summary>
public sealed record SetRecommendationRuleStatusCommand(Guid RuleId, bool IsActive) : IRequest;

/// <summary>Handles <see cref="SetRecommendationRuleStatusCommand"/>.</summary>
public sealed class SetRecommendationRuleStatusCommandHandler(IRecommendationRuleRepository repository)
    : IRequestHandler<SetRecommendationRuleStatusCommand>
{
    /// <inheritdoc/>
    public async Task Handle(SetRecommendationRuleStatusCommand request, CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(new RecommendationRuleId(request.RuleId), cancellationToken)
            ?? throw new NotFoundException(nameof(RecommendationRule), request.RuleId);

        rule.SetStatus(request.IsActive);
        await repository.UpdateAsync(rule, cancellationToken);
    }
}
