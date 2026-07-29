using Clovent.Domain;

namespace Clovent.Restaurant.KitchenTickets.Events;

/// <summary>Raised when a <see cref="KitchenTicket"/> is marked ready to serve.</summary>
public sealed record KitchenTicketMarkedReady(KitchenTicketId KitchenTicketId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
