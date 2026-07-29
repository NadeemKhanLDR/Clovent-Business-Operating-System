using Clovent.Domain;

namespace Clovent.Catalog.UnitsOfMeasure.Events;

/// <summary>Raised when a <see cref="UnitOfMeasure"/>'s display name is changed.</summary>
public sealed record UnitOfMeasureRenamed(UnitOfMeasureId UnitOfMeasureId, string Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
