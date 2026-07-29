using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Products;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>Sets or clears a product's category.</summary>
public sealed record SetProductCategoryCommand(Guid ProductId, Guid? CategoryId) : IRequest<ProductDto>;

/// <summary>Handles <see cref="SetProductCategoryCommand"/>.</summary>
public sealed class SetProductCategoryCommandHandler(IProductRepository repository) : IRequestHandler<SetProductCategoryCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(SetProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.SetCategory(request.CategoryId is { } id ? new ProductCategoryId(id) : null);
        return ProductDto.FromDomain(product);
    }
}
