using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Queries;

/// <summary>Retrieves a single product variant by its SKU.</summary>
public sealed record GetProductVariantBySkuQuery(string Sku) : IRequest<ProductVariantDto>;

/// <summary>Handles <see cref="GetProductVariantBySkuQuery"/>.</summary>
public sealed class GetProductVariantBySkuQueryHandler(IProductVariantRepository repository)
    : IRequestHandler<GetProductVariantBySkuQuery, ProductVariantDto>
{
    /// <inheritdoc/>
    public async Task<ProductVariantDto> Handle(GetProductVariantBySkuQuery request, CancellationToken cancellationToken)
    {
        var variant = await repository.GetBySkuAsync(Sku.Create(request.Sku), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), request.Sku);

        return ProductVariantDto.FromDomain(variant);
    }
}
