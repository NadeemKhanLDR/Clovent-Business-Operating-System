using Clovent.Domain;

namespace Clovent.Authentication.Credentials;

/// <summary>
/// An opaque, unpredictable marker whose entire purpose is to change value
/// whenever a security-relevant event happens to a credential (password
/// change, PIN change, etc.), so that anything which cached the previous
/// stamp (e.g. an issued session or refresh session) can be recognized as
/// stale. Generating a fresh, unguessable value is not "hashing" or
/// "encryption" - it carries no secret and is not derived from one - so
/// <see cref="Generate"/> stays in the domain rather than requiring
/// Infrastructure.
/// </summary>
public sealed class SecurityStamp : ValueObject
{
    /// <summary>The opaque marker text.</summary>
    public string Value { get; }

    private SecurityStamp(string value) => Value = value;

    /// <summary>Generates a new, unpredictable security stamp.</summary>
    public static SecurityStamp Generate() => new(Guid.NewGuid().ToString("N"));

    /// <summary>Reconstructs a security stamp from an already-generated value (e.g. read back from storage).</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty.</exception>
    public static SecurityStamp Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Security stamp is required.", nameof(value));

        return new SecurityStamp(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
