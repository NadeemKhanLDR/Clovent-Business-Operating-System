using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Products;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>Activates a product.</summary>
public sealed record ActivateProductCommand(Guid ProductId) : IRequest<ProductDto>;

/// <summary>Handles <see cref="ActivateProductCommand"/>.</summary>
public sealed class ActivateProductCommandHandler(IProductRepository repository) : IRequestHandler<ActivateProductCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(ActivateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.Activate();
        return ProductDto.FromDomain(product);
    }
}
