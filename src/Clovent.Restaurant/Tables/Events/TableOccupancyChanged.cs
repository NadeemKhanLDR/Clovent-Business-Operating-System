using Clovent.Domain;

namespace Clovent.Restaurant.Tables.Events;

/// <summary>Raised when a <see cref="Table"/>'s <see cref="TableOccupancyStatus"/> changes.</summary>
public sealed record TableOccupancyChanged(TableId TableId, TableOccupancyStatus OccupancyStatus, DateTimeOffset OccurredOnUtc) : IDomainEvent;
