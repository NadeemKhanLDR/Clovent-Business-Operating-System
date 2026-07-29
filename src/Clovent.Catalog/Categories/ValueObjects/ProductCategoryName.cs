using Clovent.Domain;

namespace Clovent.Catalog.Categories.ValueObjects;

/// <summary>A product category's display name (e.g. "Beverages").</summary>
public sealed class ProductCategoryName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private ProductCategoryName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="ProductCategoryName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>100</c> characters.</exception>
    public static ProductCategoryName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Category name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Category name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new ProductCategoryName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
