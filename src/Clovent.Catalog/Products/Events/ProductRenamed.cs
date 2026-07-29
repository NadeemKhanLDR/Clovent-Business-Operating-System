using Clovent.Catalog.Products.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Products.Events;

/// <summary>Raised when a <see cref="Product"/> is renamed.</summary>
public sealed record ProductRenamed(ProductId ProductId, ProductName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
