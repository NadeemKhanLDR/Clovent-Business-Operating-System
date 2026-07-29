using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Products;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Queries;

/// <summary>Retrieves every variant belonging to a product.</summary>
public sealed record ListProductVariantsByProductQuery(Guid ProductId) : IRequest<IReadOnlyCollection<ProductVariantDto>>;

/// <summary>Handles <see cref="ListProductVariantsByProductQuery"/>.</summary>
public sealed class ListProductVariantsByProductQueryHandler(IProductVariantRepository repository)
    : IRequestHandler<ListProductVariantsByProductQuery, IReadOnlyCollection<ProductVariantDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductVariantDto>> Handle(ListProductVariantsByProductQuery request, CancellationToken cancellationToken)
    {
        var variants = await repository.GetByProductIdAsync(new ProductId(request.ProductId), cancellationToken);
        return [.. variants.Select(v => ProductVariantDto.FromDomain(v))];
    }
}
