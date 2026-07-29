using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Products;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Queries;

/// <summary>Retrieves every product variant in the catalog, enriched with its owning product's category (feeds the POS category-button filter).</summary>
public sealed record ListProductVariantsQuery : IRequest<IReadOnlyCollection<ProductVariantDto>>;

/// <summary>Handles <see cref="ListProductVariantsQuery"/>.</summary>
public sealed class ListProductVariantsQueryHandler(IProductVariantRepository repository, IProductRepository productRepository)
    : IRequestHandler<ListProductVariantsQuery, IReadOnlyCollection<ProductVariantDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductVariantDto>> Handle(ListProductVariantsQuery request, CancellationToken cancellationToken)
    {
        var variants = await repository.GetAllAsync(cancellationToken);
        var products = await productRepository.GetAllAsync(cancellationToken);
        var categoryIdsByProductId = products.ToDictionary(p => p.Id, p => p.CategoryId?.Value);

        return
        [
            .. variants.Select(variant => ProductVariantDto.FromDomain(
                variant,
                categoryIdsByProductId.GetValueOrDefault(variant.ProductId))),
        ];
    }
}
