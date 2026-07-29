using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Prices.Queries;

/// <summary>Retrieves every price record (cost and selling, active and inactive) for a variant.</summary>
public sealed record ListProductPricesByVariantQuery(Guid ProductVariantId) : IRequest<IReadOnlyCollection<ProductPriceDto>>;

/// <summary>Handles <see cref="ListProductPricesByVariantQuery"/>.</summary>
public sealed class ListProductPricesByVariantQueryHandler(IProductPriceRepository repository)
    : IRequestHandler<ListProductPricesByVariantQuery, IReadOnlyCollection<ProductPriceDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductPriceDto>> Handle(ListProductPricesByVariantQuery request, CancellationToken cancellationToken)
    {
        var prices = await repository.GetByProductVariantIdAsync(new ProductVariantId(request.ProductVariantId), cancellationToken);
        return [.. prices.Select(ProductPriceDto.FromDomain)];
    }
}
