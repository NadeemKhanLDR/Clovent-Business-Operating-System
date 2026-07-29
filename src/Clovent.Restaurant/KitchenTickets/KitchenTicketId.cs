namespace Clovent.Restaurant.KitchenTickets;

/// <summary>Strongly-typed identifier for a <see cref="KitchenTicket"/> aggregate.</summary>
public readonly record struct KitchenTicketId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("KitchenTicketId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="KitchenTicketId"/>.</summary>
    public static KitchenTicketId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
