using Clovent.Catalog.Groups;
using Clovent.Domain;

namespace Clovent.Catalog.Products.Events;

/// <summary>Raised when a <see cref="Product"/>'s group is changed.</summary>
public sealed record ProductGroupAssigned(ProductId ProductId, ProductGroupId? GroupId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
