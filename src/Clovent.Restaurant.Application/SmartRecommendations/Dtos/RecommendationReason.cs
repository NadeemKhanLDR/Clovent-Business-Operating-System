namespace Clovent.Restaurant.Application.SmartRecommendations.Dtos;

/// <summary>Why a particular variant was suggested for the current basket.</summary>
public enum RecommendationReason
{
    /// <summary>A back-office-configured <c>RecommendationRule</c> matched the basket.</summary>
    ConfiguredRule,

    /// <summary>The variant sold the most (by quantity) across today's completed orders.</summary>
    PopularToday,

    /// <summary>Reserved for basket-co-occurrence suggestions; not produced by the deterministic v1 selector.</summary>
    FrequentlyBoughtTogether
}
