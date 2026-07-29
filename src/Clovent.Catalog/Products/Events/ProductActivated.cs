using Clovent.Domain;

namespace Clovent.Catalog.Products.Events;

/// <summary>Raised when a <see cref="Product"/> is (re)activated.</summary>
public sealed record ProductActivated(ProductId ProductId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
