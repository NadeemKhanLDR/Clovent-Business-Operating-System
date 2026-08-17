using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Categories;
using MediatR;

namespace Clovent.Catalog.Application.Categories.Commands;

/// <summary>Sets a product category's manual display position for owner-driven drag-drop reordering.</summary>
public sealed record SetProductCategorySortOrderCommand(Guid ProductCategoryId, int SortOrder) : IRequest<ProductCategoryDto>;

/// <summary>Handles <see cref="SetProductCategorySortOrderCommand"/>.</summary>
public sealed class SetProductCategorySortOrderCommandHandler(IProductCategoryRepository repository)
    : IRequestHandler<SetProductCategorySortOrderCommand, ProductCategoryDto>
{
    /// <inheritdoc/>
    public async Task<ProductCategoryDto> Handle(SetProductCategorySortOrderCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(new ProductCategoryId(request.ProductCategoryId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), request.ProductCategoryId);

        category.SetSortOrder(request.SortOrder);

        return ProductCategoryDto.FromDomain(category);
    }
}
