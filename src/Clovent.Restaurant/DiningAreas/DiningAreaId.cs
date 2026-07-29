namespace Clovent.Restaurant.DiningAreas;

/// <summary>Strongly-typed identifier for a <see cref="DiningArea"/> aggregate.</summary>
public readonly record struct DiningAreaId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("DiningAreaId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="DiningAreaId"/>.</summary>
    public static DiningAreaId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
