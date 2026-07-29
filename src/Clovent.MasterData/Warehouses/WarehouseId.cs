namespace Clovent.MasterData.Warehouses;

/// <summary>Strongly-typed identifier for a <see cref="Warehouse"/> aggregate.</summary>
public readonly record struct WarehouseId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("WarehouseId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="WarehouseId"/>.</summary>
    public static WarehouseId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
