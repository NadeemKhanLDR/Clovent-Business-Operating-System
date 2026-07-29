namespace Clovent.MasterData.Terminals;

/// <summary>Strongly-typed identifier for a <see cref="Terminal"/> aggregate.</summary>
public readonly record struct TerminalId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("TerminalId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="TerminalId"/>.</summary>
    public static TerminalId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
