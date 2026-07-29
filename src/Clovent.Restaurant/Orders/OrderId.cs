namespace Clovent.Restaurant.Orders;

/// <summary>Strongly-typed identifier for an <see cref="Order"/> aggregate.</summary>
public readonly record struct OrderId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("OrderId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="OrderId"/>.</summary>
    public static OrderId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
