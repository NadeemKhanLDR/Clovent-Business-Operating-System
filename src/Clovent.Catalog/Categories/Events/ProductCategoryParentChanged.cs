using Clovent.Domain;

namespace Clovent.Catalog.Categories.Events;

/// <summary>Raised when a <see cref="ProductCategory"/>'s parent category is changed.</summary>
public sealed record ProductCategoryParentChanged(ProductCategoryId ProductCategoryId, ProductCategoryId? ParentCategoryId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
