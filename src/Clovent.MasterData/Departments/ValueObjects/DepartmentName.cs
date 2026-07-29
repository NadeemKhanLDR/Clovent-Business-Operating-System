using Clovent.Domain;

namespace Clovent.MasterData.Departments.ValueObjects;

/// <summary>A department's display name (e.g. "Kitchen", "Front of House", "Accounting").</summary>
public sealed class DepartmentName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private DepartmentName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="DepartmentName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>100</c> characters.</exception>
    public static DepartmentName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Department name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Department name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new DepartmentName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
