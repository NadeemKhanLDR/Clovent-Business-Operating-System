using Clovent.Catalog.Categories;
using Clovent.Domain;

namespace Clovent.Catalog.Products.Events;

/// <summary>Raised when a <see cref="Product"/>'s category is changed.</summary>
public sealed record ProductCategoryAssigned(ProductId ProductId, ProductCategoryId? CategoryId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
