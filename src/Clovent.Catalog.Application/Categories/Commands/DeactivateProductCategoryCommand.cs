using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Categories;
using MediatR;

namespace Clovent.Catalog.Application.Categories.Commands;

/// <summary>Deactivates a product category.</summary>
public sealed record DeactivateProductCategoryCommand(Guid ProductCategoryId) : IRequest<ProductCategoryDto>;

/// <summary>Handles <see cref="DeactivateProductCategoryCommand"/>.</summary>
public sealed class DeactivateProductCategoryCommandHandler(IProductCategoryRepository repository)
    : IRequestHandler<DeactivateProductCategoryCommand, ProductCategoryDto>
{
    /// <inheritdoc/>
    public async Task<ProductCategoryDto> Handle(DeactivateProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(new ProductCategoryId(request.ProductCategoryId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), request.ProductCategoryId);

        category.Deactivate();

        return ProductCategoryDto.FromDomain(category);
    }
}
