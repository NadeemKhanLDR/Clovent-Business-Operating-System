namespace Clovent.Restaurant.Outbox;

/// <summary>Strongly-typed identifier for an <see cref="OutboxMessage"/> aggregate.</summary>
public readonly record struct OutboxMessageId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("OutboxMessageId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="OutboxMessageId"/>.</summary>
    public static OutboxMessageId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
