using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Categories;
using MediatR;

namespace Clovent.Catalog.Application.Categories.Commands;

/// <summary>Activates a product category.</summary>
public sealed record ActivateProductCategoryCommand(Guid ProductCategoryId) : IRequest<ProductCategoryDto>;

/// <summary>Handles <see cref="ActivateProductCategoryCommand"/>.</summary>
public sealed class ActivateProductCategoryCommandHandler(IProductCategoryRepository repository)
    : IRequestHandler<ActivateProductCategoryCommand, ProductCategoryDto>
{
    /// <inheritdoc/>
    public async Task<ProductCategoryDto> Handle(ActivateProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(new ProductCategoryId(request.ProductCategoryId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), request.ProductCategoryId);

        category.Activate();

        return ProductCategoryDto.FromDomain(category);
    }
}
