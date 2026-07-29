using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Queries;

/// <summary>Retrieves a single product variant by identity.</summary>
public sealed record GetProductVariantByIdQuery(Guid ProductVariantId) : IRequest<ProductVariantDto>;

/// <summary>Handles <see cref="GetProductVariantByIdQuery"/>.</summary>
public sealed class GetProductVariantByIdQueryHandler(IProductVariantRepository repository)
    : IRequestHandler<GetProductVariantByIdQuery, ProductVariantDto>
{
    /// <inheritdoc/>
    public async Task<ProductVariantDto> Handle(GetProductVariantByIdQuery request, CancellationToken cancellationToken)
    {
        var variant = await repository.GetByIdAsync(new ProductVariantId(request.ProductVariantId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), request.ProductVariantId);

        return ProductVariantDto.FromDomain(variant);
    }
}
