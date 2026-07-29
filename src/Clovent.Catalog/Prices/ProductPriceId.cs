namespace Clovent.Catalog.Prices;

/// <summary>Strongly-typed identifier for a <see cref="ProductPrice"/> aggregate.</summary>
public readonly record struct ProductPriceId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("ProductPriceId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="ProductPriceId"/>.</summary>
    public static ProductPriceId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
