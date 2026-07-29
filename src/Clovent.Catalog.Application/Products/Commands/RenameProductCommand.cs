using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>Renames an existing product.</summary>
public sealed record RenameProductCommand(Guid ProductId, string Name) : IRequest<ProductDto>;

/// <summary>Handles <see cref="RenameProductCommand"/>.</summary>
public sealed class RenameProductCommandHandler(IProductRepository repository) : IRequestHandler<RenameProductCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(RenameProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.Rename(ProductName.Create(request.Name));
        return ProductDto.FromDomain(product);
    }
}
