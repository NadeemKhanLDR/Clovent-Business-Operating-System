using Clovent.Domain;

namespace Clovent.Identity.Roles.ValueObjects;

/// <summary>A role's human-readable name (e.g. "Branch Manager").</summary>
public sealed class RoleName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 64;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private RoleName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="RoleName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>64</c> characters.</exception>
    public static RoleName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Role name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Role name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new RoleName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
