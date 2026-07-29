using Clovent.Domain;

namespace Clovent.Restaurant.KitchenTickets.Events;

/// <summary>Raised when preparation of a <see cref="KitchenTicket"/> begins.</summary>
public sealed record KitchenTicketStarted(KitchenTicketId KitchenTicketId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
