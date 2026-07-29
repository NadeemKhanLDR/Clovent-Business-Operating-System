using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Categories;
using MediatR;

namespace Clovent.Catalog.Application.Categories.Queries;

/// <summary>Retrieves a single product category by identity.</summary>
public sealed record GetProductCategoryByIdQuery(Guid ProductCategoryId) : IRequest<ProductCategoryDto>;

/// <summary>Handles <see cref="GetProductCategoryByIdQuery"/>.</summary>
public sealed class GetProductCategoryByIdQueryHandler(IProductCategoryRepository repository)
    : IRequestHandler<GetProductCategoryByIdQuery, ProductCategoryDto>
{
    /// <inheritdoc/>
    public async Task<ProductCategoryDto> Handle(GetProductCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(new ProductCategoryId(request.ProductCategoryId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), request.ProductCategoryId);

        return ProductCategoryDto.FromDomain(category);
    }
}
