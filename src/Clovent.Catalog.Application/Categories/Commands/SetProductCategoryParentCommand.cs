using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Categories;
using MediatR;

namespace Clovent.Catalog.Application.Categories.Commands;

/// <summary>Sets or clears a product category's parent.</summary>
public sealed record SetProductCategoryParentCommand(Guid ProductCategoryId, Guid? ParentCategoryId) : IRequest<ProductCategoryDto>;

/// <summary>Handles <see cref="SetProductCategoryParentCommand"/>.</summary>
public sealed class SetProductCategoryParentCommandHandler(IProductCategoryRepository repository)
    : IRequestHandler<SetProductCategoryParentCommand, ProductCategoryDto>
{
    /// <inheritdoc/>
    public async Task<ProductCategoryDto> Handle(SetProductCategoryParentCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(new ProductCategoryId(request.ProductCategoryId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), request.ProductCategoryId);

        var parentId = request.ParentCategoryId is { } id ? new ProductCategoryId(id) : (ProductCategoryId?)null;
        category.SetParent(parentId);

        return ProductCategoryDto.FromDomain(category);
    }
}
