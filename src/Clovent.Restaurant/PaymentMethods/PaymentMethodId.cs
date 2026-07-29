namespace Clovent.Restaurant.PaymentMethods;

/// <summary>Strongly-typed identifier for a <see cref="PaymentMethod"/> aggregate.</summary>
public readonly record struct PaymentMethodId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("PaymentMethodId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="PaymentMethodId"/>.</summary>
    public static PaymentMethodId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
