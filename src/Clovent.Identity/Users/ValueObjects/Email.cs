using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.Identity.Users.ValueObjects;

/// <summary>A validated, normalized (lowercased, trimmed) email address.</summary>
public sealed partial class Email : ValueObject
{
    private const int MaxLength = 254;

    /// <summary>The normalized address.</summary>
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>Validates and normalizes <paramref name="value"/> into an <see cref="Email"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty, too long, or not a valid email address.</exception>
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email address is required.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
            throw new ArgumentException($"Email address cannot exceed {MaxLength} characters.", nameof(value));

        if (!EmailPattern().IsMatch(normalized))
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));

        return new Email(normalized);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
