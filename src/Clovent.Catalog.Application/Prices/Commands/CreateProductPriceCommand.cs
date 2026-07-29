using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Currencies;
using MediatR;

namespace Clovent.Catalog.Application.Prices.Commands;

/// <summary>Creates a new price record for a product variant.</summary>
public sealed record CreateProductPriceCommand(Guid ProductVariantId, PriceType PriceType, decimal Amount, Guid CurrencyId) : IRequest<ProductPriceDto>;

/// <summary>Handles <see cref="CreateProductPriceCommand"/>.</summary>
public sealed class CreateProductPriceCommandHandler(IProductPriceRepository repository) : IRequestHandler<CreateProductPriceCommand, ProductPriceDto>
{
    /// <inheritdoc/>
    public async Task<ProductPriceDto> Handle(CreateProductPriceCommand request, CancellationToken cancellationToken)
    {
        var price = ProductPrice.Create(
            new ProductVariantId(request.ProductVariantId),
            request.PriceType,
            request.Amount,
            new CurrencyId(request.CurrencyId));

        await repository.AddAsync(price, cancellationToken);

        return ProductPriceDto.FromDomain(price);
    }
}
