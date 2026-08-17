using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Prices;
using MediatR;

namespace Clovent.Catalog.Application.Prices.Queries;

/// <summary>
/// Retrieves every currently-active price record of the given type, across
/// every variant, in one call - added so a screen scoping many variants at
/// once (POS's product tile wall, Menu Items) can resolve every item's
/// current price without one <see cref="ListProductPricesByVariantQuery"/>
/// per variant at load time.
/// </summary>
public sealed record ListActiveProductPricesByTypeQuery(PriceType PriceType) : IRequest<IReadOnlyCollection<ProductPriceDto>>;

/// <summary>Handles <see cref="ListActiveProductPricesByTypeQuery"/>.</summary>
public sealed class ListActiveProductPricesByTypeQueryHandler(IProductPriceRepository repository)
    : IRequestHandler<ListActiveProductPricesByTypeQuery, IReadOnlyCollection<ProductPriceDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductPriceDto>> Handle(ListActiveProductPricesByTypeQuery request, CancellationToken cancellationToken)
    {
        var prices = await repository.GetActiveByPriceTypeAsync(request.PriceType, cancellationToken);
        return [.. prices.Select(ProductPriceDto.FromDomain)];
    }
}
