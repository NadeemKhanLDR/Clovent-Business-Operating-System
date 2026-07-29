using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.MasterData.Currencies;

namespace Clovent.Catalog.Prices.Events;

/// <summary>Raised when a new <see cref="ProductPrice"/> is created.</summary>
public sealed record ProductPriceCreated(ProductPriceId ProductPriceId, ProductVariantId ProductVariantId, PriceType PriceType, decimal Amount, CurrencyId CurrencyId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
