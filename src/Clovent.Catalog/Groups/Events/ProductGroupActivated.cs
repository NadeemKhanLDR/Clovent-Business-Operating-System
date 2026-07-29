using Clovent.Domain;

namespace Clovent.Catalog.Groups.Events;

/// <summary>Raised when a <see cref="ProductGroup"/> is (re)activated.</summary>
public sealed record ProductGroupActivated(ProductGroupId ProductGroupId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
