using Clovent.Domain;

namespace Clovent.Identity.Branches.ValueObjects;

/// <summary>A branch/location's display name (e.g. "Downtown").</summary>
public sealed class BranchName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 200;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private BranchName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="BranchName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>200</c> characters.</exception>
    public static BranchName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Branch name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Branch name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new BranchName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
