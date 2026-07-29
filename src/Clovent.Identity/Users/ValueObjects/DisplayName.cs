using Clovent.Domain;

namespace Clovent.Identity.Users.ValueObjects;

/// <summary>
/// The name shown for a user throughout the product - independently
/// changeable from their <see cref="PersonName"/>, e.g. a preferred name or nickname.
/// </summary>
public sealed class DisplayName : ValueObject
{
    /// <summary>The maximum permitted length.</summary>
    public const int MaxLength = 100;

    /// <summary>The display text.</summary>
    public string Value { get; }

    private DisplayName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="DisplayName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or exceeds <see cref="MaxLength"/> characters.</exception>
    public static DisplayName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Display name is required.", nameof(value));

        value = value.Trim();

        if (value.Length > MaxLength)
            throw new ArgumentException($"Display name cannot exceed {MaxLength} characters.", nameof(value));

        return new DisplayName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
