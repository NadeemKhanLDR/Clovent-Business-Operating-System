using Clovent.Domain;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when an <see cref="Order"/> is completed (fully paid and closed).</summary>
public sealed record OrderCompleted(OrderId OrderId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
