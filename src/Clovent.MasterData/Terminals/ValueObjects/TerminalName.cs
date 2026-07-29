using Clovent.Domain;

namespace Clovent.MasterData.Terminals.ValueObjects;

/// <summary>A terminal's display name (e.g. "Front Counter 1", "Drive-Thru").</summary>
public sealed class TerminalName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    /// <summary>The name text.</summary>
    public string Value { get; }

    private TerminalName(string value) => Value = value;

    /// <summary>Validates <paramref name="value"/> into a <see cref="TerminalName"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is shorter than <c>2</c> or longer than <c>100</c> characters.</exception>
    public static TerminalName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Terminal name is required.", nameof(value));

        value = value.Trim();

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException($"Terminal name must be {MinLength}-{MaxLength} characters.", nameof(value));

        return new TerminalName(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
