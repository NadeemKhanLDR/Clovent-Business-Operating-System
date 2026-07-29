using Clovent.Domain;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when an <see cref="Order"/>'s internal notes change.</summary>
public sealed record OrderNotesChanged(OrderId OrderId, string? Notes, DateTimeOffset OccurredOnUtc) : IDomainEvent;
