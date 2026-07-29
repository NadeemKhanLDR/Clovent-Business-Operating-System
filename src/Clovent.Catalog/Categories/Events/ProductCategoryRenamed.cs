using Clovent.Catalog.Categories.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Categories.Events;

/// <summary>Raised when a <see cref="ProductCategory"/> is renamed.</summary>
public sealed record ProductCategoryRenamed(ProductCategoryId ProductCategoryId, ProductCategoryName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
