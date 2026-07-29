using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Categories;
using MediatR;

namespace Clovent.Catalog.Application.Categories.Queries;

/// <summary>Retrieves every product category.</summary>
public sealed record ListProductCategoriesQuery : IRequest<IReadOnlyCollection<ProductCategoryDto>>;

/// <summary>Handles <see cref="ListProductCategoriesQuery"/>.</summary>
public sealed class ListProductCategoriesQueryHandler(IProductCategoryRepository repository)
    : IRequestHandler<ListProductCategoriesQuery, IReadOnlyCollection<ProductCategoryDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductCategoryDto>> Handle(ListProductCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await repository.GetAllAsync(cancellationToken);
        return [.. categories.Select(ProductCategoryDto.FromDomain)];
    }
}
