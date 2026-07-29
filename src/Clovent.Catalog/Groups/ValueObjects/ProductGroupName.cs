using Clovent.Domain;

namespace Clovent.Catalog.Groups.ValueObjects;

/// <summary>A product group's display name (e.g. "Soft Drinks").</summary>
public sealed class ProductGroupName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private ProductGroupName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="ProductGroupName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>100</c> characters.</exception>
    public static ProductGroupName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Group name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Group name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new ProductGroupName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
