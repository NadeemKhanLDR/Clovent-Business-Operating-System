using Clovent.Catalog.Categories.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Categories.Events;

/// <summary>Raised when a new <see cref="ProductCategory"/> is created.</summary>
public sealed record ProductCategoryCreated(ProductCategoryId ProductCategoryId, ProductCategoryName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
