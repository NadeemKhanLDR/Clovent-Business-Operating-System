using Clovent.Domain;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.OrderLines.Events;

/// <summary>Raised when an <see cref="OrderLine"/> moves to a different <see cref="Order"/> (a table split/merge).</summary>
public sealed record OrderLineTransferredToOrder(OrderLineId OrderLineId, OrderId OrderId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
