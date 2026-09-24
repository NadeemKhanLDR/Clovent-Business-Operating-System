using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using MediatR;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// One pickable product variant in a Smart POS editor dialog: the owning
/// product's name, the variant's name, and the variant's current selling
/// price - the same catalog read model the POS product wall resolves through.
/// </summary>
public sealed record ProductOptionRow(Guid VariantId, Guid ProductId, string ProductName, string VariantName, decimal UnitPrice)
{
    /// <summary>The picker caption: product name alone for single-variant products, otherwise "Product - Variant".</summary>
    public string Display => VariantName.Length > 0 && VariantName != ProductName
        ? $"{ProductName} - {VariantName}"
        : ProductName;
}

/// <summary>One pickable product (for the recommendation rule's optional trigger product).</summary>
public sealed record ProductOptionRowSummary(Guid ProductId, string ProductName);

/// <summary>
/// Loads the shared product/variant/price option list every Smart POS dialog
/// picks from, so the rule and template editors resolve the exact same catalog
/// view the POS sells through.
/// </summary>
public static class SmartPosCatalogOptions
{
    /// <summary>Loads every active-catalog variant with its product name and current selling price, ordered for display.</summary>
    public static async Task<IReadOnlyList<ProductOptionRow>> LoadAsync(IMediator mediator, CancellationToken cancellationToken = default)
    {
        var variants = await mediator.Send(new ListProductVariantsQuery(), cancellationToken);
        var products = await mediator.Send(new ListProductsQuery(), cancellationToken);
        var sellingPrices = await mediator.Send(new ListActiveProductPricesByTypeQuery(PriceType.Selling), cancellationToken);

        var productNameByProductId = products.ToDictionary(p => p.ProductId, p => p.Name);
        var unitPriceByVariantId = sellingPrices
            .GroupBy(p => p.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFromUtc).First().Amount);

        var rows = variants
            .Where(v => string.Equals(v.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Select(v => new ProductOptionRow(
                v.ProductVariantId,
                v.ProductId,
                productNameByProductId.GetValueOrDefault(v.ProductId, "(unknown product)"),
                v.Name,
                unitPriceByVariantId.GetValueOrDefault(v.ProductVariantId, 0m)))
            .OrderBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.VariantName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return rows;
    }

    /// <summary>Collapses variant options into the distinct product list a trigger-product picker shows.</summary>
    public static IReadOnlyList<ProductOptionRowSummary> ToProductSummaries(IReadOnlyList<ProductOptionRow> options)
    {
        var summaries = options
            .GroupBy(o => o.ProductId)
            .Select(g => new ProductOptionRowSummary(g.Key, g.First().ProductName))
            .OrderBy(s => s.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // The "any basket" sentinel - an empty product id means the rule fires
        // regardless of what the basket contains.
        summaries.Insert(0, new ProductOptionRowSummary(Guid.Empty, "(Any Product)"));
        return summaries;
    }
}
