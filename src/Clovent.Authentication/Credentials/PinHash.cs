using Clovent.Domain;

namespace Clovent.Authentication.Credentials;

/// <summary>
/// An opaque, already-computed PIN hash. Kept as its own type rather than
/// reusing <see cref="PasswordHash"/> for the same reason
/// <c>Clovent.Identity</c>'s per-aggregate name value objects are kept
/// distinct from each other: type safety against accidentally storing one
/// credential's hash where the other belongs, at the cost of a small amount
/// of duplication. Never computes a hash - see <see cref="PasswordHash"/>.
/// </summary>
public sealed class PinHash : ValueObject
{
    /// <summary>The opaque hash text, exactly as produced by whatever hashing algorithm computed it.</summary>
    public string Value { get; }

    private PinHash(string value) => Value = value;

    /// <summary>Wraps an already-computed hash value.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty.</exception>
    public static PinHash Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PIN hash is required.", nameof(value));

        return new PinHash(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
