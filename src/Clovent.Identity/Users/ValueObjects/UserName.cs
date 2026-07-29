using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.Identity.Users.ValueObjects;

/// <summary>
/// A user's login handle. Distinct from <see cref="DisplayName"/> (what other
/// users see) and from any authentication credential - this domain models
/// only the identifier's shape, not how it is used to sign in.
/// </summary>
public sealed partial class UserName : ValueObject
{
    private const int MinLength = 3;
    private const int MaxLength = 32;

    /// <summary>The handle as entered (case preserved for display; equality is case-insensitive).</summary>
    public string Value { get; }

    private UserName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="UserName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> does not match the required shape.</exception>
    public static UserName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Username is required.", nameof(value));

        var trimmed = value.Trim();

        if (!UserNamePattern().IsMatch(trimmed))
            throw new ArgumentException(
                $"Username must be {MinLength}-{MaxLength} characters, start with a letter, and contain only letters, digits, '.', '_' or '-'.",
                nameof(value));

        return new UserName(trimmed);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9._-]{2,31}$")]
    private static partial Regex UserNamePattern();
}
