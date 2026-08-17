using Clovent.Domain;

namespace Clovent.Catalog.Categories.Events;

/// <summary>Raised when a <see cref="ProductCategory"/>'s display color changes.</summary>
public sealed record ProductCategoryColorChanged(ProductCategoryId ProductCategoryId, string? ColorHex, DateTimeOffset OccurredOnUtc) : IDomainEvent;
