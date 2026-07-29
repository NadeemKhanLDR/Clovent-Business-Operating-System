using Clovent.Domain;

namespace Clovent.Restaurant.DiningAreas.ValueObjects;

/// <summary>A dining area's display name (e.g. "Patio", "Main Hall").</summary>
public sealed class DiningAreaName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private DiningAreaName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="DiningAreaName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>100</c> characters.</exception>
    public static DiningAreaName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Dining area name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Dining area name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new DiningAreaName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
