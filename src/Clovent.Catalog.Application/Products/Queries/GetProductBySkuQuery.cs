using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Products;
using Clovent.Catalog.Shared.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Products.Queries;

/// <summary>Retrieves a single product by its SKU.</summary>
public sealed record GetProductBySkuQuery(string Sku) : IRequest<ProductDto>;

/// <summary>Handles <see cref="GetProductBySkuQuery"/>.</summary>
public sealed class GetProductBySkuQueryHandler(IProductRepository repository) : IRequestHandler<GetProductBySkuQuery, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(GetProductBySkuQuery request, CancellationToken cancellationToken)
    {
        var product = await repository.GetBySkuAsync(Sku.Create(request.Sku), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Sku);

        return ProductDto.FromDomain(product);
    }
}
