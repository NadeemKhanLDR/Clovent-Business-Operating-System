using Clovent.Domain;

namespace Clovent.Catalog.Products.ValueObjects;

/// <summary>A product's display name (e.g. "Espresso Beans 1kg").</summary>
public sealed class ProductName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 200;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private ProductName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="ProductName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>200</c> characters.</exception>
    public static ProductName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Product name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Product name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new ProductName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
