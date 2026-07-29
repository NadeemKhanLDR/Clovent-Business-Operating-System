using Clovent.Domain;

namespace Clovent.Identity.Companies.ValueObjects;

/// <summary>A company's registered/trading name.</summary>
public sealed class CompanyName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 200;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private CompanyName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="CompanyName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>200</c> characters.</exception>
    public static CompanyName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Company name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Company name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new CompanyName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
