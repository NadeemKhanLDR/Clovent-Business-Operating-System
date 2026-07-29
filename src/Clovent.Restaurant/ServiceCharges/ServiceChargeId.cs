namespace Clovent.Restaurant.ServiceCharges;

/// <summary>Strongly-typed identifier for a <see cref="ServiceCharge"/> aggregate.</summary>
public readonly record struct ServiceChargeId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("ServiceChargeId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="ServiceChargeId"/>.</summary>
    public static ServiceChargeId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
