using Clovent.Domain;

namespace Clovent.MasterData.FiscalYears.ValueObjects;

/// <summary>A fiscal year's display label (e.g. "FY2026", "2026-2027").</summary>
public sealed class FiscalYearName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 50;

    /// <summary>The label text.</summary>
    public string Value { get; }

    private FiscalYearName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="FiscalYearName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>50</c> characters.</exception>
    public static FiscalYearName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Fiscal year name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Fiscal year name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new FiscalYearName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
