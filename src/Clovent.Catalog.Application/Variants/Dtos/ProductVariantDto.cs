using Clovent.Catalog.Variants;

namespace Clovent.Catalog.Application.Variants.Dtos;

/// <summary>Read-model shape for a <see cref="ProductVariant"/>, safe to cross a process boundary.</summary>
public sealed record ProductVariantDto(
    Guid ProductVariantId,
    Guid ProductId,
    string Name,
    string Sku,
    Guid UnitOfMeasureId,
    string Status,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="ProductVariant"/> into its DTO.</summary>
    public static ProductVariantDto FromDomain(ProductVariant variant) => new(
        variant.Id.Value,
        variant.ProductId.Value,
        variant.Name.Value,
        variant.Sku.Value,
        variant.UnitOfMeasureId.Value,
        variant.Status.ToString(),
        variant.CreatedAtUtc);
}
