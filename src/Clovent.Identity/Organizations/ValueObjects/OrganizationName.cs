using Clovent.Domain;

namespace Clovent.Identity.Organizations.ValueObjects;

/// <summary>An organization's registered/trading name.</summary>
public sealed class OrganizationName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 200;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private OrganizationName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into an <see cref="OrganizationName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>200</c> characters.</exception>
    public static OrganizationName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Organization name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Organization name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new OrganizationName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
