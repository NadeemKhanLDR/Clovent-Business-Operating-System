using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Prices;
using MediatR;

namespace Clovent.Catalog.Application.Prices.Commands;

/// <summary>Activates a price record.</summary>
public sealed record ActivateProductPriceCommand(Guid ProductPriceId) : IRequest<ProductPriceDto>;

/// <summary>Handles <see cref="ActivateProductPriceCommand"/>.</summary>
public sealed class ActivateProductPriceCommandHandler(IProductPriceRepository repository)
    : IRequestHandler<ActivateProductPriceCommand, ProductPriceDto>
{
    /// <inheritdoc/>
    public async Task<ProductPriceDto> Handle(ActivateProductPriceCommand request, CancellationToken cancellationToken)
    {
        var price = await repository.GetByIdAsync(new ProductPriceId(request.ProductPriceId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductPrice), request.ProductPriceId);

        price.Activate();
        return ProductPriceDto.FromDomain(price);
    }
}
