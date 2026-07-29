using Clovent.Domain;

namespace Clovent.Catalog.Prices.Events;

/// <summary>Raised when a <see cref="ProductPrice"/>'s amount is changed.</summary>
public sealed record ProductPriceAmountChanged(ProductPriceId ProductPriceId, decimal Amount, DateTimeOffset OccurredOnUtc) : IDomainEvent;
