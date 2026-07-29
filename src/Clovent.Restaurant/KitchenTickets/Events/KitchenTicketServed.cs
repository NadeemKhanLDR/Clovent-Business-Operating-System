using Clovent.Domain;

namespace Clovent.Restaurant.KitchenTickets.Events;

/// <summary>Raised when a <see cref="KitchenTicket"/> is served.</summary>
public sealed record KitchenTicketServed(KitchenTicketId KitchenTicketId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
