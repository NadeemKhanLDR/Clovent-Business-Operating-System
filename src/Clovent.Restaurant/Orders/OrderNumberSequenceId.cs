namespace Clovent.Restaurant.Orders;

/// <summary>Strongly-typed identifier for the <see cref="OrderNumberSequence"/> aggregate.</summary>
public readonly record struct OrderNumberSequenceId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("OrderNumberSequenceId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="OrderNumberSequenceId"/>.</summary>
    public static OrderNumberSequenceId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
