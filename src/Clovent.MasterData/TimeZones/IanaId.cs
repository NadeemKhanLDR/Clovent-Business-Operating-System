using Clovent.Domain;

namespace Clovent.MasterData.TimeZones;

/// <summary>
/// An IANA time zone database identifier (e.g. "America/New_York", "UTC") -
/// validated only for shape (non-empty, reasonable length), not cross-checked
/// against the actual IANA database, since .NET's own time zone data varies
/// by OS/ICU version and this milestone models the reference-data concept,
/// not a live time zone conversion engine.
/// </summary>
public sealed class IanaId : ValueObject
{
    private const int MaxLength = 100;

    /// <summary>The identifier text (e.g. "America/New_York").</summary>
    public string Value { get; }

    private IanaId(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into an <see cref="IanaId"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or exceeds <see cref="MaxLength"/> characters.</exception>
    public static IanaId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Time zone id is required.", nameof(value));

        value = value.Trim();

        if (value.Length > MaxLength)
            throw new ArgumentException($"Time zone id cannot exceed {MaxLength} characters.", nameof(value));

        return new IanaId(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
