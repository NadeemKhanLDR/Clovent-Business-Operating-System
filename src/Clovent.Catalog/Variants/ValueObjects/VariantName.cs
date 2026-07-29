using Clovent.Domain;

namespace Clovent.Catalog.Variants.ValueObjects;

/// <summary>A product variant's display name/attribute summary (e.g. "Size: Large, Color: Red").</summary>
public sealed class VariantName : ValueObject
{
    private const int MinLength = 1;
    private const int MaxLength = 200;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private VariantName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="VariantName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or longer than <c>200</c> characters.</exception>
    public static VariantName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Variant name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Variant name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new VariantName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
