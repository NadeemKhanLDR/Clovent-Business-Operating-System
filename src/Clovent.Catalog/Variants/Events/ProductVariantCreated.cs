using Clovent.Catalog.Products;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.Variants.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Variants.Events;

/// <summary>Raised when a new <see cref="ProductVariant"/> is created.</summary>
public sealed record ProductVariantCreated(ProductVariantId ProductVariantId, ProductId ProductId, VariantName Name, Sku Sku, DateTimeOffset OccurredOnUtc) : IDomainEvent;
