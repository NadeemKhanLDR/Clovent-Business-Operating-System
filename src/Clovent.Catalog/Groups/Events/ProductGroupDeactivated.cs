using Clovent.Domain;

namespace Clovent.Catalog.Groups.Events;

/// <summary>Raised when a <see cref="ProductGroup"/> is deactivated.</summary>
public sealed record ProductGroupDeactivated(ProductGroupId ProductGroupId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
