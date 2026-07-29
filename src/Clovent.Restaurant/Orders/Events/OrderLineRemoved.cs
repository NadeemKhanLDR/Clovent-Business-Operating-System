using Clovent.Domain;
using Clovent.Restaurant.OrderLines;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when an <see cref="OrderLines.OrderLine"/> is removed from an <see cref="Order"/>.</summary>
public sealed record OrderLineRemoved(OrderId OrderId, OrderLineId OrderLineId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
