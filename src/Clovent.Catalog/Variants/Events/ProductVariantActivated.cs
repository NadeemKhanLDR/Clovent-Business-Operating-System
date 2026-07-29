using Clovent.Domain;

namespace Clovent.Catalog.Variants.Events;

/// <summary>Raised when a <see cref="ProductVariant"/> is (re)activated.</summary>
public sealed record ProductVariantActivated(ProductVariantId ProductVariantId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
