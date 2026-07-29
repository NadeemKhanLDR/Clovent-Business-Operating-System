namespace Clovent.Restaurant.Tables;

/// <summary>Strongly-typed identifier for a <see cref="Table"/> aggregate.</summary>
public readonly record struct TableId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("TableId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="TableId"/>.</summary>
    public static TableId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
