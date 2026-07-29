namespace Clovent.Restaurant.Sales;

/// <summary>Strongly-typed identifier for a <see cref="DailySalesSequence"/> aggregate.</summary>
public readonly record struct DailySalesSequenceId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("DailySalesSequenceId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="DailySalesSequenceId"/>.</summary>
    public static DailySalesSequenceId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
