namespace Clovent.Restaurant.Refunds;

/// <summary>Strongly-typed identifier for a <see cref="RefundLine"/>.</summary>
public readonly record struct RefundLineId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("RefundLineId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="RefundLineId"/>.</summary>
    public static RefundLineId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
