namespace Clovent.MasterData.TimeZones;

/// <summary>Strongly-typed identifier for a <see cref="TimeZoneEntry"/> aggregate.</summary>
public readonly record struct TimeZoneEntryId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("TimeZoneEntryId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="TimeZoneEntryId"/>.</summary>
    public static TimeZoneEntryId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
