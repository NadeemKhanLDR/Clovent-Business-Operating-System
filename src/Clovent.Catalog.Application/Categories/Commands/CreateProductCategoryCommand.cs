using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Categories.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Categories.Commands;

/// <summary>Creates a new product category.</summary>
public sealed record CreateProductCategoryCommand(string Name, Guid? ParentCategoryId = null) : IRequest<ProductCategoryDto>;

/// <summary>Handles <see cref="CreateProductCategoryCommand"/>.</summary>
public sealed class CreateProductCategoryCommandHandler(IProductCategoryRepository repository)
    : IRequestHandler<CreateProductCategoryCommand, ProductCategoryDto>
{
    /// <inheritdoc/>
    public async Task<ProductCategoryDto> Handle(CreateProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var parentId = request.ParentCategoryId is { } id ? new ProductCategoryId(id) : (ProductCategoryId?)null;
        var category = ProductCategory.Create(ProductCategoryName.Create(request.Name), parentId);

        await repository.AddAsync(category, cancellationToken);

        return ProductCategoryDto.FromDomain(category);
    }
}
