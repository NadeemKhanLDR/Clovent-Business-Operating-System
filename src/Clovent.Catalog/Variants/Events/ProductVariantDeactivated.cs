using Clovent.Domain;

namespace Clovent.Catalog.Variants.Events;

/// <summary>Raised when a <see cref="ProductVariant"/> is deactivated.</summary>
public sealed record ProductVariantDeactivated(ProductVariantId ProductVariantId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
