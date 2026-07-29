using Clovent.Domain;

namespace Clovent.Identity.Shared.ValueObjects;

/// <summary>A physical mailing address, structured but deliberately country-agnostic (no jurisdiction-specific field rules).</summary>
public sealed class Address : ValueObject
{
    private const int MaxFieldLength = 200;

    /// <summary>Street line (number, name, unit).</summary>
    public string Street { get; }

    /// <summary>City/town.</summary>
    public string City { get; }

    /// <summary>State/province/region.</summary>
    public string State { get; }

    /// <summary>Postal/ZIP code.</summary>
    public string PostalCode { get; }

    /// <summary>Country.</summary>
    public string Country { get; }

    private Address(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    /// <summary>Validates and normalizes the given fields into an <see cref="Address"/>. Every field is required.</summary>
    /// <exception cref="ArgumentException">A field is empty or exceeds <c>200</c> characters.</exception>
    public static Address Create(string street, string city, string state, string postalCode, string country)
    {
        return new Address(
            RequireField(street, nameof(street)),
            RequireField(city, nameof(city)),
            RequireField(state, nameof(state)),
            RequireField(postalCode, nameof(postalCode)),
            RequireField(country, nameof(country)));
    }

    private static string RequireField(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Address {fieldName} is required.", fieldName);

        value = value.Trim();

        if (value.Length > MaxFieldLength)
            throw new ArgumentException($"Address {fieldName} cannot exceed {MaxFieldLength} characters.", fieldName);

        return value;
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Street}, {City}, {State} {PostalCode}, {Country}";
}
