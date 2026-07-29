using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.MasterData.Languages;

/// <summary>An ISO 639-1 two-letter language code (e.g. "en", "es").</summary>
public sealed partial class LanguageCode : ValueObject
{
    /// <summary>The code, always two lowercase letters.</summary>
    public string Value { get; }

    private LanguageCode(string value) => Value = value;

    /// <summary>Validates and normalizes <paramref name="value"/> into a <see cref="LanguageCode"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not exactly two letters.</exception>
    public static LanguageCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Language code is required.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!CodePattern().IsMatch(normalized))
            throw new ArgumentException($"'{value}' is not a valid ISO 639-1 language code (expected two letters, e.g. 'en').", nameof(value));

        return new LanguageCode(normalized);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z]{2}$")]
    private static partial Regex CodePattern();
}
