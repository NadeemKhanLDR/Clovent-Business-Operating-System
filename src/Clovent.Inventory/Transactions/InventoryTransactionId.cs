namespace Clovent.Inventory.Transactions;

/// <summary>Strongly-typed identifier for an <see cref="InventoryTransaction"/> aggregate.</summary>
public readonly record struct InventoryTransactionId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("InventoryTransactionId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="InventoryTransactionId"/>.</summary>
    public static InventoryTransactionId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
