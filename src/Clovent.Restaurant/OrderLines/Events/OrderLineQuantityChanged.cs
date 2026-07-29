using Clovent.Domain;

namespace Clovent.Restaurant.OrderLines.Events;

/// <summary>Raised when an <see cref="OrderLine"/>'s quantity changes.</summary>
public sealed record OrderLineQuantityChanged(OrderLineId OrderLineId, decimal Quantity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
