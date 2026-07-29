using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Prices;
using MediatR;

namespace Clovent.Catalog.Application.Prices.Queries;

/// <summary>Retrieves a single price record by identity.</summary>
public sealed record GetProductPriceByIdQuery(Guid ProductPriceId) : IRequest<ProductPriceDto>;

/// <summary>Handles <see cref="GetProductPriceByIdQuery"/>.</summary>
public sealed class GetProductPriceByIdQueryHandler(IProductPriceRepository repository)
    : IRequestHandler<GetProductPriceByIdQuery, ProductPriceDto>
{
    /// <inheritdoc/>
    public async Task<ProductPriceDto> Handle(GetProductPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var price = await repository.GetByIdAsync(new ProductPriceId(request.ProductPriceId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductPrice), request.ProductPriceId);

        return ProductPriceDto.FromDomain(price);
    }
}
