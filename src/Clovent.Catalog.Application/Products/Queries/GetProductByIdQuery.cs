using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Products;
using MediatR;

namespace Clovent.Catalog.Application.Products.Queries;

/// <summary>Retrieves a single product by identity.</summary>
public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto>;

/// <summary>Handles <see cref="GetProductByIdQuery"/>.</summary>
public sealed class GetProductByIdQueryHandler(IProductRepository repository) : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        return ProductDto.FromDomain(product);
    }
}
