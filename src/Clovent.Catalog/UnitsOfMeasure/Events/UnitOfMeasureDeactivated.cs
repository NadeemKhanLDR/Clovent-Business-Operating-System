using Clovent.Domain;

namespace Clovent.Catalog.UnitsOfMeasure.Events;

/// <summary>Raised when a <see cref="UnitOfMeasure"/> is deactivated.</summary>
public sealed record UnitOfMeasureDeactivated(UnitOfMeasureId UnitOfMeasureId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
