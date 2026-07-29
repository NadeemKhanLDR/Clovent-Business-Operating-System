using Clovent.Domain;

namespace Clovent.Restaurant.DiningAreas.Events;

/// <summary>Raised when a <see cref="DiningArea"/> is (re)activated.</summary>
public sealed record DiningAreaActivated(DiningAreaId DiningAreaId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
