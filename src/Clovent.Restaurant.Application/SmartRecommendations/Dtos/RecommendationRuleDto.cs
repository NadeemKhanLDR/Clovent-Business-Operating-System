using Clovent.Restaurant.SmartRecommendations;

namespace Clovent.Restaurant.Application.SmartRecommendations.Dtos;

/// <summary>Read-model shape for a <see cref="RecommendationRule"/> on the Back Office management screen.</summary>
public sealed record RecommendationRuleDto(
    Guid RuleId,
    Guid? ProductId,
    Guid RecommendedVariantId,
    int Priority,
    bool IsActive,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    int? DaysOfWeek,
    string? Notes)
{
    /// <summary>Projects a domain <see cref="RecommendationRule"/> into its DTO.</summary>
    public static RecommendationRuleDto FromDomain(RecommendationRule rule) => new(
        rule.Id.Value,
        rule.ProductId,
        rule.RecommendedVariantId.Value,
        rule.Priority,
        rule.IsActive,
        rule.StartTime,
        rule.EndTime,
        rule.DaysOfWeek,
        rule.Notes);
}
