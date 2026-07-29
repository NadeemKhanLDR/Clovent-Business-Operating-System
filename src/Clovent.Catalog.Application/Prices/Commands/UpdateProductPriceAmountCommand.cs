using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Prices;
using MediatR;

namespace Clovent.Catalog.Application.Prices.Commands;

/// <summary>Updates a price record's amount.</summary>
public sealed record UpdateProductPriceAmountCommand(Guid ProductPriceId, decimal Amount) : IRequest<ProductPriceDto>;

/// <summary>Handles <see cref="UpdateProductPriceAmountCommand"/>.</summary>
public sealed class UpdateProductPriceAmountCommandHandler(IProductPriceRepository repository)
    : IRequestHandler<UpdateProductPriceAmountCommand, ProductPriceDto>
{
    /// <inheritdoc/>
    public async Task<ProductPriceDto> Handle(UpdateProductPriceAmountCommand request, CancellationToken cancellationToken)
    {
        var price = await repository.GetByIdAsync(new ProductPriceId(request.ProductPriceId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductPrice), request.ProductPriceId);

        price.UpdateAmount(request.Amount);
        return ProductPriceDto.FromDomain(price);
    }
}
