using Clovent.Domain;

namespace Clovent.Restaurant.KitchenTickets.Events;

/// <summary>Raised when a <see cref="KitchenTicket"/> is cancelled.</summary>
public sealed record KitchenTicketCancelled(KitchenTicketId KitchenTicketId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
