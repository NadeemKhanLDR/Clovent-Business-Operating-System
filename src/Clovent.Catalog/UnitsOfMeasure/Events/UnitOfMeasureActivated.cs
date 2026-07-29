using Clovent.Domain;

namespace Clovent.Catalog.UnitsOfMeasure.Events;

/// <summary>Raised when a <see cref="UnitOfMeasure"/> is (re)activated.</summary>
public sealed record UnitOfMeasureActivated(UnitOfMeasureId UnitOfMeasureId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
