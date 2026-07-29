namespace Clovent.Restaurant.OrderLines;

/// <summary>Strongly-typed identifier for an <see cref="OrderLine"/> aggregate.</summary>
public readonly record struct OrderLineId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("OrderLineId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="OrderLineId"/>.</summary>
    public static OrderLineId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
