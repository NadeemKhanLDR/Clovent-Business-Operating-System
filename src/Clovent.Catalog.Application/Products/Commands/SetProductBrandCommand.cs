using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Brands;
using Clovent.Catalog.Products;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>Sets or clears a product's brand.</summary>
public sealed record SetProductBrandCommand(Guid ProductId, Guid? BrandId) : IRequest<ProductDto>;

/// <summary>Handles <see cref="SetProductBrandCommand"/>.</summary>
public sealed class SetProductBrandCommandHandler(IProductRepository repository) : IRequestHandler<SetProductBrandCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(SetProductBrandCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.SetBrand(request.BrandId is { } id ? new BrandId(id) : null);
        return ProductDto.FromDomain(product);
    }
}
