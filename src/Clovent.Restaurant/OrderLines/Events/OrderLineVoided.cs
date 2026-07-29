using Clovent.Domain;

namespace Clovent.Restaurant.OrderLines.Events;

/// <summary>Raised when an <see cref="OrderLine"/> is voided.</summary>
public sealed record OrderLineVoided(OrderLineId OrderLineId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
