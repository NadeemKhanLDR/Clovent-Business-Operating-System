using Clovent.Catalog.Application.Groups.Dtos;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Groups.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Groups.Commands;

/// <summary>Creates a new product group.</summary>
public sealed record CreateProductGroupCommand(string Name) : IRequest<ProductGroupDto>;

/// <summary>Handles <see cref="CreateProductGroupCommand"/>.</summary>
public sealed class CreateProductGroupCommandHandler(IProductGroupRepository repository)
    : IRequestHandler<CreateProductGroupCommand, ProductGroupDto>
{
    /// <inheritdoc/>
    public async Task<ProductGroupDto> Handle(CreateProductGroupCommand request, CancellationToken cancellationToken)
    {
        var group = ProductGroup.Create(ProductGroupName.Create(request.Name));
        await repository.AddAsync(group, cancellationToken);
        return ProductGroupDto.FromDomain(group);
    }
}
