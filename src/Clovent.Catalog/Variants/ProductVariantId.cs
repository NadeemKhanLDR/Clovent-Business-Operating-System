namespace Clovent.Catalog.Variants;

/// <summary>Strongly-typed identifier for a <see cref="ProductVariant"/> aggregate.</summary>
public readonly record struct ProductVariantId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("ProductVariantId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="ProductVariantId"/>.</summary>
    public static ProductVariantId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
