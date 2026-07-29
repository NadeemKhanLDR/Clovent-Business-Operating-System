using Clovent.Catalog.Application.Groups.Dtos;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Groups.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Groups.Commands;

/// <summary>Renames an existing product group.</summary>
public sealed record RenameProductGroupCommand(Guid ProductGroupId, string Name) : IRequest<ProductGroupDto>;

/// <summary>Handles <see cref="RenameProductGroupCommand"/>.</summary>
public sealed class RenameProductGroupCommandHandler(IProductGroupRepository repository)
    : IRequestHandler<RenameProductGroupCommand, ProductGroupDto>
{
    /// <inheritdoc/>
    public async Task<ProductGroupDto> Handle(RenameProductGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(new ProductGroupId(request.ProductGroupId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductGroup), request.ProductGroupId);

        group.Rename(ProductGroupName.Create(request.Name));
        return ProductGroupDto.FromDomain(group);
    }
}
