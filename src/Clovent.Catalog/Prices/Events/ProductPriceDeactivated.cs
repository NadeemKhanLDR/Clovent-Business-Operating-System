using Clovent.Domain;

namespace Clovent.Catalog.Prices.Events;

/// <summary>Raised when a <see cref="ProductPrice"/> is deactivated.</summary>
public sealed record ProductPriceDeactivated(ProductPriceId ProductPriceId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
