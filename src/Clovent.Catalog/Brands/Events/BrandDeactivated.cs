using Clovent.Domain;

namespace Clovent.Catalog.Brands.Events;

/// <summary>Raised when a <see cref="Brand"/> is deactivated.</summary>
public sealed record BrandDeactivated(BrandId BrandId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
