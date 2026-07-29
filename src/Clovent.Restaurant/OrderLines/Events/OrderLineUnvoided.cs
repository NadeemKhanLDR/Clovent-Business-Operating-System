using Clovent.Domain;

namespace Clovent.Restaurant.OrderLines.Events;

/// <summary>Raised when a voided <see cref="OrderLine"/> is restored.</summary>
public sealed record OrderLineUnvoided(OrderLineId OrderLineId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
