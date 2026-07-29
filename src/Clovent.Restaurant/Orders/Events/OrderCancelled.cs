using Clovent.Domain;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when an <see cref="Order"/> is cancelled.</summary>
public sealed record OrderCancelled(OrderId OrderId, string Reason, DateTimeOffset OccurredOnUtc) : IDomainEvent;
