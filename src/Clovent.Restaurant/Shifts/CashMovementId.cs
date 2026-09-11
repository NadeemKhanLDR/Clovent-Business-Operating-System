namespace Clovent.Restaurant.Shifts;

/// <summary>Strongly-typed identifier for a <see cref="CashMovement"/> entity.</summary>
public readonly record struct CashMovementId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("CashMovementId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="CashMovementId"/>.</summary>
    public static CashMovementId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
