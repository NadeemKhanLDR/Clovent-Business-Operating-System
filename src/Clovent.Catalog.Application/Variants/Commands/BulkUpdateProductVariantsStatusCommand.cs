using Clovent.Catalog.Products;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Commands;

/// <summary>
/// Bulk activates or deactivates multiple product variants and their parent products in a single transaction.
/// </summary>
public sealed record BulkUpdateProductVariantsStatusCommand(
    IReadOnlyList<Guid> VariantIds,
    bool Activate) : IRequest<int>;

/// <summary>Handles <see cref="BulkUpdateProductVariantsStatusCommand"/>.</summary>
public sealed class BulkUpdateProductVariantsStatusCommandHandler(
    IProductVariantRepository variantRepository,
    IProductRepository productRepository) : IRequestHandler<BulkUpdateProductVariantsStatusCommand, int>
{
    /// <inheritdoc/>
    public async Task<int> Handle(BulkUpdateProductVariantsStatusCommand request, CancellationToken cancellationToken)
    {
        if (request.VariantIds.Count == 0)
        {
            return 0;
        }

        int affectedCount = 0;
        var touchedProductIds = new HashSet<ProductId>();

        foreach (var id in request.VariantIds)
        {
            var variant = await variantRepository.GetByIdAsync(new ProductVariantId(id), cancellationToken);
            if (variant is null) continue;

            if (request.Activate && variant.Status != CatalogStatus.Active)
            {
                variant.Activate();
                affectedCount++;
                touchedProductIds.Add(variant.ProductId);
            }
            else if (!request.Activate && variant.Status != CatalogStatus.Inactive)
            {
                variant.Deactivate();
                affectedCount++;
                touchedProductIds.Add(variant.ProductId);
            }
        }

        foreach (var productId in touchedProductIds)
        {
            var product = await productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is null) continue;

            var productVariants = await variantRepository.GetByProductIdAsync(productId, cancellationToken);
            bool hasActiveVariant = productVariants.Any(v => v.Status == CatalogStatus.Active);

            if (hasActiveVariant && product.Status != CatalogStatus.Active)
            {
                product.Activate();
            }
            else if (!hasActiveVariant && product.Status != CatalogStatus.Inactive)
            {
                product.Deactivate();
            }
        }

        return affectedCount;
    }
}
