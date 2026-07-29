using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.MasterData.Currencies;

/// <summary>An ISO 4217 three-letter currency code (e.g. "USD", "EUR").</summary>
public sealed partial class CurrencyCode : ValueObject
{
    /// <summary>The code, always three uppercase letters.</summary>
    public string Value { get; }

    private CurrencyCode(string value) => Value = value;

    /// <summary>Validates and normalizes <paramref name="value"/> into a <see cref="CurrencyCode"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not exactly three letters.</exception>
    public static CurrencyCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency code is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();

        if (!CodePattern().IsMatch(normalized))
            throw new ArgumentException($"'{value}' is not a valid ISO 4217 currency code (expected three letters, e.g. 'USD').", nameof(value));

        return new CurrencyCode(normalized);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Z]{3}$")]
    private static partial Regex CodePattern();
}
