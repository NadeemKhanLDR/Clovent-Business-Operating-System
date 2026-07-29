using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Domain;

namespace Clovent.Catalog.Variants.Events;

/// <summary>Raised when a <see cref="ProductVariant"/>'s unit of measure is changed.</summary>
public sealed record ProductVariantUnitOfMeasureChanged(ProductVariantId ProductVariantId, UnitOfMeasureId UnitOfMeasureId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
