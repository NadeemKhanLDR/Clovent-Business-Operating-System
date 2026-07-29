using Clovent.Catalog.Groups.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Groups.Events;

/// <summary>Raised when a <see cref="ProductGroup"/> is renamed.</summary>
public sealed record ProductGroupRenamed(ProductGroupId ProductGroupId, ProductGroupName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
