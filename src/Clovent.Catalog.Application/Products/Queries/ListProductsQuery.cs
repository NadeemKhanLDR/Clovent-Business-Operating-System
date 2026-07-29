using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Products;
using MediatR;

namespace Clovent.Catalog.Application.Products.Queries;

/// <summary>Retrieves every product.</summary>
public sealed record ListProductsQuery : IRequest<IReadOnlyCollection<ProductDto>>;

/// <summary>Handles <see cref="ListProductsQuery"/>.</summary>
public sealed class ListProductsQueryHandler(IProductRepository repository) : IRequestHandler<ListProductsQuery, IReadOnlyCollection<ProductDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await repository.GetAllAsync(cancellationToken);
        return [.. products.Select(ProductDto.FromDomain)];
    }
}
