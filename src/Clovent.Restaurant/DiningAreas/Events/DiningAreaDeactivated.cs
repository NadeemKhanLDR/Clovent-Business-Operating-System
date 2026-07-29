using Clovent.Domain;

namespace Clovent.Restaurant.DiningAreas.Events;

/// <summary>Raised when a <see cref="DiningArea"/> is deactivated.</summary>
public sealed record DiningAreaDeactivated(DiningAreaId DiningAreaId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
