namespace Clovent.Catalog.Products;

/// <summary>Strongly-typed identifier for a <see cref="Product"/> aggregate.</summary>
public readonly record struct ProductId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("ProductId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="ProductId"/>.</summary>
    public static ProductId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
