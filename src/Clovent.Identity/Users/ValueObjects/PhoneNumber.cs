using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.Identity.Users.ValueObjects;

/// <summary>A validated phone number in loose E.164-style form.</summary>
public sealed partial class PhoneNumber : ValueObject
{
    /// <summary>The normalized number, digits only with an optional leading '+'.</summary>
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    /// <summary>Validates and normalizes <paramref name="value"/> into a <see cref="PhoneNumber"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or not a valid phone number.</exception>
    public static PhoneNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number is required.", nameof(value));

        var normalized = value.Trim().Replace(" ", "").Replace("-", "");

        if (!PhoneNumberPattern().IsMatch(normalized))
            throw new ArgumentException($"'{value}' is not a valid phone number.", nameof(value));

        return new PhoneNumber(normalized);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^\+?[1-9]\d{6,14}$")]
    private static partial Regex PhoneNumberPattern();
}
