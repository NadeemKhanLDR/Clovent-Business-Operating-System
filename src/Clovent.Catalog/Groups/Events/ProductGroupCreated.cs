using Clovent.Catalog.Groups.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Groups.Events;

/// <summary>Raised when a new <see cref="ProductGroup"/> is created.</summary>
public sealed record ProductGroupCreated(ProductGroupId ProductGroupId, ProductGroupName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
