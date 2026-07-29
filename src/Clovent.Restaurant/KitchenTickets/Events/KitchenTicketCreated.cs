using Clovent.Domain;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.KitchenTickets.Events;

/// <summary>Raised when a new <see cref="KitchenTicket"/> is sent to the kitchen.</summary>
public sealed record KitchenTicketCreated(KitchenTicketId KitchenTicketId, OrderId OrderId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
