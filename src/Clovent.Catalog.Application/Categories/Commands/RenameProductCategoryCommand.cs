using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Categories.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Categories.Commands;

/// <summary>Renames an existing product category.</summary>
public sealed record RenameProductCategoryCommand(Guid ProductCategoryId, string Name) : IRequest<ProductCategoryDto>;

/// <summary>Handles <see cref="RenameProductCategoryCommand"/>.</summary>
public sealed class RenameProductCategoryCommandHandler(IProductCategoryRepository repository)
    : IRequestHandler<RenameProductCategoryCommand, ProductCategoryDto>
{
    /// <inheritdoc/>
    public async Task<ProductCategoryDto> Handle(RenameProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(new ProductCategoryId(request.ProductCategoryId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), request.ProductCategoryId);

        category.Rename(ProductCategoryName.Create(request.Name));

        return ProductCategoryDto.FromDomain(category);
    }
}
