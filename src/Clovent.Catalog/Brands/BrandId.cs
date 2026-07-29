namespace Clovent.Catalog.Brands;

/// <summary>Strongly-typed identifier for a <see cref="Brand"/> aggregate.</summary>
public readonly record struct BrandId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("BrandId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="BrandId"/>.</summary>
    public static BrandId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
