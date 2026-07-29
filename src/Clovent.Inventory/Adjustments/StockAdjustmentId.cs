namespace Clovent.Inventory.Adjustments;

/// <summary>Strongly-typed identifier for a <see cref="StockAdjustment"/> aggregate.</summary>
public readonly record struct StockAdjustmentId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("StockAdjustmentId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="StockAdjustmentId"/>.</summary>
    public static StockAdjustmentId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
