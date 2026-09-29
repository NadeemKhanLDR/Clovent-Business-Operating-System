using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Products;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>Updates the item classification type (Prepared, PurchasedResale, Service) of a product.</summary>
public sealed record SetProductItemTypeCommand(
    Guid ProductId,
    ProductItemType ItemType) : IRequest<ProductDto>;

/// <summary>Handles <see cref="SetProductItemTypeCommand"/>.</summary>
public sealed class SetProductItemTypeCommandHandler(IProductRepository repository)
    : IRequestHandler<SetProductItemTypeCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(SetProductItemTypeCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.SetItemType(request.ItemType);
        return ProductDto.FromDomain(product);
    }
}
