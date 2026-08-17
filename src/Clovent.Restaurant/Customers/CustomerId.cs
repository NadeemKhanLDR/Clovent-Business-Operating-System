namespace Clovent.Restaurant.Customers;

/// <summary>Strongly-typed identifier for a <see cref="Customer"/> aggregate.</summary>
public readonly record struct CustomerId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("CustomerId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="CustomerId"/>.</summary>
    public static CustomerId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
