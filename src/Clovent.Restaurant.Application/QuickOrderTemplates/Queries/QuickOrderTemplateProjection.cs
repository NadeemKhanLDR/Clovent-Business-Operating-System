using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Clovent.Restaurant.QuickOrderTemplates;
using MediatR;

namespace Clovent.Restaurant.Application.QuickOrderTemplates.Queries;

/// <summary>
/// Shared catalog read-model lookup both template list queries expand
/// <see cref="QuickOrderTemplate"/> items through: variant names, owning
/// product names, and each variant's current selling price - the same price
/// read model the POS product wall resolves through.
/// </summary>
internal static class QuickOrderTemplateProjection
{
    /// <summary>
    /// Loads the catalog lookups from the same mediator the calling handler
    /// was resolved with, so template expansion never depends on a second DI scope.
    /// </summary>
    public static async Task<Lookup> CreateAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        var variants = await mediator.Send(new ListProductVariantsQuery(), cancellationToken);
        var products = await mediator.Send(new ListProductsQuery(), cancellationToken);
        var sellingPrices = await mediator.Send(new ListActiveProductPricesByTypeQuery(Clovent.Catalog.Prices.PriceType.Selling), cancellationToken);

        return new Lookup(
            variants.ToDictionary(v => v.ProductVariantId),
            products.ToDictionary(p => p.ProductId, p => p.Name),
            sellingPrices
                .GroupBy(p => p.ProductVariantId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFromUtc).First().Amount));
    }

    /// <summary>The immutable catalog lookups one expansion pass runs against.</summary>
    internal sealed record Lookup(
        IReadOnlyDictionary<Guid, Clovent.Catalog.Application.Variants.Dtos.ProductVariantDto> VariantById,
        IReadOnlyDictionary<Guid, string> ProductNameByProductId,
        IReadOnlyDictionary<Guid, decimal> UnitPriceByVariantId)
    {
        /// <summary>Expands one template (with its items) into its display DTO.</summary>
        public QuickOrderTemplateDto Expand(QuickOrderTemplate template)
        {
            var items = template.Items
                .Select(ExpandItem)
                .ToList();

            return new QuickOrderTemplateDto(
                template.Id.Value,
                template.Name,
                template.Description,
                template.IsActive,
                template.DisplayOrder,
                items,
                items.Sum(i => i.Total), template.WarehouseId);
        }

        private QuickOrderTemplateItemDto ExpandItem(QuickOrderTemplateItem item)
        {
            var variantId = item.VariantId.Value;
            VariantById.TryGetValue(variantId, out var variant);
            var productName = variant is not null && ProductNameByProductId.TryGetValue(variant.ProductId, out var name) ? name : "(unknown product)";

            return new QuickOrderTemplateItemDto(
                variantId,
                productName,
                variant?.Name ?? "(unknown variant)",
                item.Quantity,
                item.TemplateUnitPrice ?? UnitPriceByVariantId.GetValueOrDefault(variantId, 0m),
                item.TemplateUnitPrice);
        }
    }
}

