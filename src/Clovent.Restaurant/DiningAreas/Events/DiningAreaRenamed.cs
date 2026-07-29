using Clovent.Domain;
using Clovent.Restaurant.DiningAreas.ValueObjects;

namespace Clovent.Restaurant.DiningAreas.Events;

/// <summary>Raised when a <see cref="DiningArea"/>'s name changes.</summary>
public sealed record DiningAreaRenamed(DiningAreaId DiningAreaId, DiningAreaName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
