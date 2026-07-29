using Clovent.Catalog.Brands.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Brands.Events;

/// <summary>Raised when a new <see cref="Brand"/> is created.</summary>
public sealed record BrandCreated(BrandId BrandId, BrandName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
