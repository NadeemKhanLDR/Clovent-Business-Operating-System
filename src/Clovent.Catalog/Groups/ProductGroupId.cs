namespace Clovent.Catalog.Groups;

/// <summary>Strongly-typed identifier for a <see cref="ProductGroup"/> aggregate.</summary>
public readonly record struct ProductGroupId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("ProductGroupId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="ProductGroupId"/>.</summary>
    public static ProductGroupId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
