namespace Clovent.Restaurant.Payments;

/// <summary>Strongly-typed identifier for a <see cref="Payment"/> aggregate.</summary>
public readonly record struct PaymentId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("PaymentId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="PaymentId"/>.</summary>
    public static PaymentId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
