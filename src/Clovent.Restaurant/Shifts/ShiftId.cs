namespace Clovent.Restaurant.Shifts;

/// <summary>Strongly-typed identifier for a <see cref="Shift"/> aggregate.</summary>
public readonly record struct ShiftId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("ShiftId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="ShiftId"/>.</summary>
    public static ShiftId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
