using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Products;
using MediatR;

namespace Clovent.Catalog.Application.Products.Commands;

/// <summary>Sets or clears a product's group.</summary>
public sealed record SetProductGroupCommand(Guid ProductId, Guid? GroupId) : IRequest<ProductDto>;

/// <summary>Handles <see cref="SetProductGroupCommand"/>.</summary>
public sealed class SetProductGroupCommandHandler(IProductRepository repository) : IRequestHandler<SetProductGroupCommand, ProductDto>
{
    /// <inheritdoc/>
    public async Task<ProductDto> Handle(SetProductGroupCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.SetGroup(request.GroupId is { } id ? new ProductGroupId(id) : null);
        return ProductDto.FromDomain(product);
    }
}
