using Clovent.Domain;

namespace Clovent.Catalog.Prices.Events;

/// <summary>Raised when a <see cref="ProductPrice"/> is (re)activated.</summary>
public sealed record ProductPriceActivated(ProductPriceId ProductPriceId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
