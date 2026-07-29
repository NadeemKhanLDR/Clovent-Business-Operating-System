using Clovent.Catalog.Application.Groups.Dtos;
using Clovent.Catalog.Groups;
using MediatR;

namespace Clovent.Catalog.Application.Groups.Queries;

/// <summary>Retrieves a single product group by identity.</summary>
public sealed record GetProductGroupByIdQuery(Guid ProductGroupId) : IRequest<ProductGroupDto>;

/// <summary>Handles <see cref="GetProductGroupByIdQuery"/>.</summary>
public sealed class GetProductGroupByIdQueryHandler(IProductGroupRepository repository)
    : IRequestHandler<GetProductGroupByIdQuery, ProductGroupDto>
{
    /// <inheritdoc/>
    public async Task<ProductGroupDto> Handle(GetProductGroupByIdQuery request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(new ProductGroupId(request.ProductGroupId), cancellationToken)
            ?? throw new NotFoundException(nameof(ProductGroup), request.ProductGroupId);

        return ProductGroupDto.FromDomain(group);
    }
}
