using Clovent.Domain;

namespace Clovent.Restaurant.Tables.Events;

/// <summary>Raised when a <see cref="Table"/>'s seating capacity changes.</summary>
public sealed record TableCapacityChanged(TableId TableId, int Capacity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
