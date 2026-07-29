using Clovent.Domain;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when an <see cref="Order"/>'s customer-facing notes change.</summary>
public sealed record OrderCustomerNotesChanged(OrderId OrderId, string? CustomerNotes, DateTimeOffset OccurredOnUtc) : IDomainEvent;
