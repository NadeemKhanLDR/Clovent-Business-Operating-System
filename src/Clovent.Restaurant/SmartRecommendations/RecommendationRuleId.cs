namespace Clovent.Restaurant.SmartRecommendations;

/// <summary>Strongly-typed identifier for a <see cref="RecommendationRule"/> aggregate.</summary>
public readonly record struct RecommendationRuleId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("RecommendationRuleId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="RecommendationRuleId"/>.</summary>
    public static RecommendationRuleId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
