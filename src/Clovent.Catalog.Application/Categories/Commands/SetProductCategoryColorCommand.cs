using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Categories;
using MediatR;

namespace Clovent.Catalog.Application.Categories.Commands;

/// <summary>Sets or clears a product category's display color (a "#RRGGBB" hex string, or <see langword="null"/> to clear it).</summary>
public sealed record SetProductCategoryColorCommand(Guid ProductCategoryId, string? ColorHex) : IRequest<ProductCategoryDto>;

/// <summary>Handles <see cref="SetProductCategoryColorCommand"/>.</summary>
public sealed class SetProductCategoryColorCommandHandler(IProductCategoryRepository repository)
    : IRequestHandler<SetProductCategoryColorCommand, ProductCategoryDto>
{
    /// <inheritdoc/>
    public async Task<ProductCategoryDto> Handle(SetProductCategoryColorCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(new ProductCategoryId(request.ProductCategoryId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductCategory), request.ProductCategoryId);

        category.SetColor(request.ColorHex);

        return ProductCategoryDto.FromDomain(category);
    }
}
