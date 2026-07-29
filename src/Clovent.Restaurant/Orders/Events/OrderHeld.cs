using Clovent.Domain;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when an <see cref="Order"/> is held.</summary>
public sealed record OrderHeld(OrderId OrderId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
