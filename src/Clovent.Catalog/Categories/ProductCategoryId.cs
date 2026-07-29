namespace Clovent.Catalog.Categories;

/// <summary>Strongly-typed identifier for a <see cref="ProductCategory"/> aggregate.</summary>
public readonly record struct ProductCategoryId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("ProductCategoryId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="ProductCategoryId"/>.</summary>
    public static ProductCategoryId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
