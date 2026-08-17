using Clovent.Domain;

namespace Clovent.Catalog.Variants.Events;

/// <summary>Raised when a <see cref="ProductVariant"/>'s manual display position changes.</summary>
public sealed record ProductVariantSortOrderChanged(ProductVariantId ProductVariantId, int SortOrder, DateTimeOffset OccurredOnUtc) : IDomainEvent;
