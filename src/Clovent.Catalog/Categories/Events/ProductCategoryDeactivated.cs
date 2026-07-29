using Clovent.Domain;

namespace Clovent.Catalog.Categories.Events;

/// <summary>Raised when a <see cref="ProductCategory"/> is deactivated.</summary>
public sealed record ProductCategoryDeactivated(ProductCategoryId ProductCategoryId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
