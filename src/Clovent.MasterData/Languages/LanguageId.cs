namespace Clovent.MasterData.Languages;

/// <summary>Strongly-typed identifier for a <see cref="Language"/> aggregate.</summary>
public readonly record struct LanguageId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("LanguageId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="LanguageId"/>.</summary>
    public static LanguageId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
