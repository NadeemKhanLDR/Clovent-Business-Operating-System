using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Products;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>Deactivates a product.</summary>
public sealed record DeactivateProductCommand(Guid ProductId) : IRequest<ProductDto>;

/// <summary>Handles <see cref="DeactivateProductCommand"/>.</summary>
public sealed class DeactivateProductCommandHandler(IProductRepository repository) : IRequestHandler<DeactivateProductCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.Deactivate();
        return ProductDto.FromDomain(product);
    }
}
