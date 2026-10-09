using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>Changes a product's tax configuration.</summary>
public sealed record SetProductTaxConfigurationCommand(
    Guid ProductId,
    decimal TaxRatePercentage,
    bool TaxIsInclusive,
    Clovent.Catalog.TaxProfiles.TaxClassification? TaxClassification = null,
    string? TaxCode = null,
    string? Authority = null,
    Guid? TaxProfileId = null) : IRequest<ProductDto>;

/// <summary>Handles <see cref="SetProductTaxConfigurationCommand"/>.</summary>
public sealed class SetProductTaxConfigurationCommandHandler(IProductRepository repository)
    : IRequestHandler<SetProductTaxConfigurationCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(SetProductTaxConfigurationCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var profileId = request.TaxProfileId.HasValue ? new Clovent.Catalog.TaxProfiles.TaxProfileId(request.TaxProfileId.Value) : (Clovent.Catalog.TaxProfiles.TaxProfileId?)null;
        product.SetTaxConfiguration(TaxConfiguration.Create(
            request.TaxRatePercentage,
            request.TaxIsInclusive,
            request.TaxClassification,
            request.TaxCode,
            request.Authority,
            profileId));

        return ProductDto.FromDomain(product);
    }
}
