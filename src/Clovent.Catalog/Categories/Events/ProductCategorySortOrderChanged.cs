using Clovent.Domain;

namespace Clovent.Catalog.Categories.Events;

/// <summary>Raised when a <see cref="ProductCategory"/>'s manual display position changes.</summary>
public sealed record ProductCategorySortOrderChanged(ProductCategoryId ProductCategoryId, int SortOrder, DateTimeOffset OccurredOnUtc) : IDomainEvent;
