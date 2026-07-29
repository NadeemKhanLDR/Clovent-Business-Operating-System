using Clovent.Catalog.Brands.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Brands.Events;

/// <summary>Raised when a <see cref="Brand"/> is renamed.</summary>
public sealed record BrandRenamed(BrandId BrandId, BrandName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
