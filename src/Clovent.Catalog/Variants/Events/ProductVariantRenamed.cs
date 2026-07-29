using Clovent.Catalog.Variants.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Variants.Events;

/// <summary>Raised when a <see cref="ProductVariant"/> is renamed.</summary>
public sealed record ProductVariantRenamed(ProductVariantId ProductVariantId, VariantName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
