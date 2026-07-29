using Clovent.Domain;

namespace Clovent.Catalog.Categories.Events;

/// <summary>Raised when a <see cref="ProductCategory"/> is (re)activated.</summary>
public sealed record ProductCategoryActivated(ProductCategoryId ProductCategoryId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
