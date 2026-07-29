using Clovent.Domain;

namespace Clovent.Restaurant.OrderLines.Events;

/// <summary>Raised when an <see cref="OrderLine"/>'s item notes change.</summary>
public sealed record OrderLineNotesChanged(OrderLineId OrderLineId, string? Notes, DateTimeOffset OccurredOnUtc) : IDomainEvent;
