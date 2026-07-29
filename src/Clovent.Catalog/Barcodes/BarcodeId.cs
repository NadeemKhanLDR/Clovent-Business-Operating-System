namespace Clovent.Catalog.Barcodes;

/// <summary>Strongly-typed identifier for a <see cref="Barcode"/> aggregate.</summary>
public readonly record struct BarcodeId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("BarcodeId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="BarcodeId"/>.</summary>
    public static BarcodeId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
