using Clovent.Domain;

namespace Clovent.Authentication.Credentials;

/// <summary>
/// An opaque, already-computed password hash. This is an Authentication
/// concept, not an Identity one - <c>Clovent.Identity.Users.User</c> has no
/// knowledge of credentials at all. The value object holds and compares the
/// hash only; it never computes one - producing a hash from a plaintext
/// password is a security/Infrastructure concern for a later milestone.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    /// <summary>The opaque hash text, exactly as produced by whatever hashing algorithm computed it.</summary>
    public string Value { get; }

    private PasswordHash(string value) => Value = value;

    /// <summary>Wraps an already-computed hash value.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty.</exception>
    public static PasswordHash Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Password hash is required.", nameof(value));

        return new PasswordHash(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
