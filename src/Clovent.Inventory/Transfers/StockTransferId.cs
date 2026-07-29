namespace Clovent.Inventory.Transfers;

/// <summary>Strongly-typed identifier for a <see cref="StockTransfer"/> aggregate.</summary>
public readonly record struct StockTransferId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("StockTransferId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="StockTransferId"/>.</summary>
    public static StockTransferId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
