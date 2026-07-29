using Clovent.Catalog.Brands;
using Clovent.Domain;

namespace Clovent.Catalog.Products.Events;

/// <summary>Raised when a <see cref="Product"/>'s brand is changed.</summary>
public sealed record ProductBrandAssigned(ProductId ProductId, BrandId? BrandId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
