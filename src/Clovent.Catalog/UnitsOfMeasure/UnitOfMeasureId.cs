namespace Clovent.Catalog.UnitsOfMeasure;

/// <summary>Strongly-typed identifier for a <see cref="UnitOfMeasure"/> aggregate.</summary>
public readonly record struct UnitOfMeasureId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("UnitOfMeasureId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="UnitOfMeasureId"/>.</summary>
    public static UnitOfMeasureId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
