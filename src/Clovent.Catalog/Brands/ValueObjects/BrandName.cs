using Clovent.Domain;

namespace Clovent.Catalog.Brands.ValueObjects;

/// <summary>A brand's display name (e.g. "Acme").</summary>
public sealed class BrandName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private BrandName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="BrandName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>100</c> characters.</exception>
    public static BrandName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Brand name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Brand name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new BrandName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
