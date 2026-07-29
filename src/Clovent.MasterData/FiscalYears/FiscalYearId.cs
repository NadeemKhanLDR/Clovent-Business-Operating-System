namespace Clovent.MasterData.FiscalYears;

/// <summary>Strongly-typed identifier for a <see cref="FiscalYear"/> aggregate.</summary>
public readonly record struct FiscalYearId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("FiscalYearId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="FiscalYearId"/>.</summary>
    public static FiscalYearId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
