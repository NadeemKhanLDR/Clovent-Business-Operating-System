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
    DateTimeOffset CreatedAtUtc,
    Guid? ProductCategoryId = null)
{
    /// <summary>
    /// Projects a domain <see cref="ProductVariant"/> into its DTO.
    /// <paramref name="productCategoryId"/> is the owning <see cref="Clovent.Catalog.Products.Product"/>'s
    /// category - optional (defaults to <see langword="null"/>) since most
    /// callers only have the variant loaded, not its parent product; only
    /// <c>ListProductVariantsQueryHandler</c> (POS category-button support)
    /// loads both and passes it through.
    /// </summary>
    public static ProductVariantDto FromDomain(ProductVariant variant, Guid? productCategoryId = null) => new(
        variant.Id.Value,
        variant.ProductId.Value,
        variant.Name.Value,
        variant.Sku.Value,
        variant.UnitOfMeasureId.Value,
        variant.Status.ToString(),
        variant.CreatedAtUtc,
        productCategoryId);
}
