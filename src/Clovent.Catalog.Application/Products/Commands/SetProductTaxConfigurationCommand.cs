using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>Changes a product's tax configuration.</summary>
public sealed record SetProductTaxConfigurationCommand(Guid ProductId, decimal TaxRatePercentage, bool TaxIsInclusive) : IRequest<ProductDto>;

/// <summary>Handles <see cref="SetProductTaxConfigurationCommand"/>.</summary>
public sealed class SetProductTaxConfigurationCommandHandler(IProductRepository repository)
    : IRequestHandler<SetProductTaxConfigurationCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(SetProductTaxConfigurationCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.SetTaxConfiguration(TaxConfiguration.Create(request.TaxRatePercentage, request.TaxIsInclusive));
        return ProductDto.FromDomain(product);
    }
}
