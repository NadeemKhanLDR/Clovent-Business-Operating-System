using Clovent.Domain;

namespace Clovent.Catalog.Brands.Events;

/// <summary>Raised when a <see cref="Brand"/> is (re)activated.</summary>
public sealed record BrandActivated(BrandId BrandId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
