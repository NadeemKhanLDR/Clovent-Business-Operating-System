using Clovent.Domain;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when an <see cref="Order"/> is voided.</summary>
public sealed record OrderVoided(OrderId OrderId, string Reason, DateTimeOffset OccurredOnUtc) : IDomainEvent;
