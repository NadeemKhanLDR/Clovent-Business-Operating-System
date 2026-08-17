namespace Clovent.Restaurant.ActivityLogs;

/// <summary>Strongly-typed identifier for an <see cref="ActivityLogEntry"/> aggregate.</summary>
public readonly record struct ActivityLogEntryId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("ActivityLogEntryId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="ActivityLogEntryId"/>.</summary>
    public static ActivityLogEntryId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
