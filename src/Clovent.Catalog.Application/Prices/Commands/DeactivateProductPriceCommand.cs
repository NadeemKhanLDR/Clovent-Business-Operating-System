using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Prices;
using MediatR;

namespace Clovent.Catalog.Application.Prices.Commands;

/// <summary>Deactivates a price record.</summary>
public sealed record DeactivateProductPriceCommand(Guid ProductPriceId) : IRequest<ProductPriceDto>;

/// <summary>Handles <see cref="DeactivateProductPriceCommand"/>.</summary>
public sealed class DeactivateProductPriceCommandHandler(IProductPriceRepository repository)
    : IRequestHandler<DeactivateProductPriceCommand, ProductPriceDto>
{
    /// <inheritdoc/>
    public async Task<ProductPriceDto> Handle(DeactivateProductPriceCommand request, CancellationToken cancellationToken)
    {
        var price = await repository.GetByIdAsync(new ProductPriceId(request.ProductPriceId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductPrice), request.ProductPriceId);

        price.Deactivate();
        return ProductPriceDto.FromDomain(price);
    }
}
