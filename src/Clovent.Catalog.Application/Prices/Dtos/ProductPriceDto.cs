using Clovent.Catalog.Prices;

namespace Clovent.Catalog.Application.Prices.Dtos;

/// <summary>Read-model shape for a <see cref="ProductPrice"/>, safe to cross a process boundary.</summary>
public sealed record ProductPriceDto(
    Guid ProductPriceId,
    Guid ProductVariantId,
    string PriceType,
    decimal Amount,
    Guid CurrencyId,
    DateTimeOffset EffectiveFromUtc,
    string Status,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="ProductPrice"/> into its DTO.</summary>
    public static ProductPriceDto FromDomain(ProductPrice price) => new(
        price.Id.Value,
        price.ProductVariantId.Value,
        price.PriceType.ToString(),
        price.Amount,
        price.CurrencyId.Value,
        price.EffectiveFromUtc,
        price.Status.ToString(),
        price.CreatedAtUtc);
}
