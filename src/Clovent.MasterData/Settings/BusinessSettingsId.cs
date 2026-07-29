namespace Clovent.MasterData.Settings;

/// <summary>Strongly-typed identifier for a <see cref="BusinessSettings"/> aggregate.</summary>
public readonly record struct BusinessSettingsId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("BusinessSettingsId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="BusinessSettingsId"/>.</summary>
    public static BusinessSettingsId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
