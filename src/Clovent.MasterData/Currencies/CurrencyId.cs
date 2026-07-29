namespace Clovent.MasterData.Currencies;

/// <summary>Strongly-typed identifier for a <see cref="Currency"/> aggregate.</summary>
public readonly record struct CurrencyId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("CurrencyId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="CurrencyId"/>.</summary>
    public static CurrencyId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
