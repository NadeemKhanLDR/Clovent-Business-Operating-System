using Clovent.Catalog.Application.Groups.Dtos;
using Clovent.Catalog.Groups;
using MediatR;

namespace Clovent.Catalog.Application.Groups.Commands;

/// <summary>Deactivates a product group.</summary>
public sealed record DeactivateProductGroupCommand(Guid ProductGroupId) : IRequest<ProductGroupDto>;

/// <summary>Handles <see cref="DeactivateProductGroupCommand"/>.</summary>
public sealed class DeactivateProductGroupCommandHandler(IProductGroupRepository repository)
    : IRequestHandler<DeactivateProductGroupCommand, ProductGroupDto>
{
    /// <inheritdoc/>
    public async Task<ProductGroupDto> Handle(DeactivateProductGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(new ProductGroupId(request.ProductGroupId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductGroup), request.ProductGroupId);

        group.Deactivate();
        return ProductGroupDto.FromDomain(group);
    }
}
