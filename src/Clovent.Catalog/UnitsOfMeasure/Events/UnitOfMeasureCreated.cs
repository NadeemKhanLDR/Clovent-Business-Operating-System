using Clovent.Catalog.UnitsOfMeasure.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.UnitsOfMeasure.Events;

/// <summary>Raised when a new <see cref="UnitOfMeasure"/> catalog entry is created.</summary>
public sealed record UnitOfMeasureCreated(UnitOfMeasureId UnitOfMeasureId, UnitOfMeasureCode Code, DateTimeOffset OccurredOnUtc) : IDomainEvent;
