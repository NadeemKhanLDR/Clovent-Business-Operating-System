using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Domain;

namespace Clovent.Catalog.Products.Events;

/// <summary>Raised when a new <see cref="Product"/> is created.</summary>
public sealed record ProductCreated(ProductId ProductId, ProductName Name, Sku Sku, UnitOfMeasureId BaseUnitOfMeasureId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
