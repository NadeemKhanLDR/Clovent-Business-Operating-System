using Clovent.Catalog.Products;

namespace Clovent.Catalog.Application.Products.Dtos;

/// <summary>Read-model shape for a <see cref="Product"/>, safe to cross a process boundary.</summary>
public sealed record ProductDto(
    Guid ProductId,
    string Name,
    string Sku,
    Guid? CategoryId,
    Guid? GroupId,
    Guid? BrandId,
    Guid BaseUnitOfMeasureId,
    decimal TaxRatePercentage,
    bool TaxIsInclusive,
    string Status,
    DateTimeOffset CreatedAtUtc,
    string ItemType = "Prepared",
    string TaxClassification = "Taxable",
    string? TaxCode = null,
    string? TaxAuthority = null,
    Guid? TaxProfileId = null)
{
    /// <summary>Projects a domain <see cref="Product"/> into its DTO.</summary>
    public static ProductDto FromDomain(Product product) => new(
        product.Id.Value,
        product.Name.Value,
        product.Sku.Value,
        product.CategoryId?.Value,
        product.GroupId?.Value,
        product.BrandId?.Value,
        product.BaseUnitOfMeasureId.Value,
        product.TaxConfiguration.RatePercentage,
        product.TaxConfiguration.IsInclusive,
        product.Status.ToString(),
        product.CreatedAtUtc,
        product.ItemType.ToString(),
        product.TaxConfiguration.TaxClassification.ToString(),
        product.TaxConfiguration.TaxCode,
        product.TaxConfiguration.Authority,
        product.TaxConfiguration.TaxProfileId?.Value);
}
