using Clovent.Domain;

namespace Clovent.Catalog.Products.Events;

/// <summary>Raised when a <see cref="Product"/> is deactivated.</summary>
public sealed record ProductDeactivated(ProductId ProductId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
