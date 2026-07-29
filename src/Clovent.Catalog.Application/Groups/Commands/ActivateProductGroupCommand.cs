using Clovent.Catalog.Application.Groups.Dtos;
using Clovent.Catalog.Groups;
using MediatR;

namespace Clovent.Catalog.Application.Groups.Commands;

/// <summary>Activates a product group.</summary>
public sealed record ActivateProductGroupCommand(Guid ProductGroupId) : IRequest<ProductGroupDto>;

/// <summary>Handles <see cref="ActivateProductGroupCommand"/>.</summary>
public sealed class ActivateProductGroupCommandHandler(IProductGroupRepository repository)
    : IRequestHandler<ActivateProductGroupCommand, ProductGroupDto>
{
    /// <inheritdoc/>
    public async Task<ProductGroupDto> Handle(ActivateProductGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(new ProductGroupId(request.ProductGroupId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductGroup), request.ProductGroupId);

        group.Activate();
        return ProductGroupDto.FromDomain(group);
    }
}
