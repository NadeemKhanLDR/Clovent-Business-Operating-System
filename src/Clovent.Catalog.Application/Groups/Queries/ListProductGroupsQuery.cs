using Clovent.Catalog.Application.Groups.Dtos;
using Clovent.Catalog.Groups;
using MediatR;

namespace Clovent.Catalog.Application.Groups.Queries;

/// <summary>Retrieves every product group.</summary>
public sealed record ListProductGroupsQuery : IRequest<IReadOnlyCollection<ProductGroupDto>>;

/// <summary>Handles <see cref="ListProductGroupsQuery"/>.</summary>
public sealed class ListProductGroupsQueryHandler(IProductGroupRepository repository)
    : IRequestHandler<ListProductGroupsQuery, IReadOnlyCollection<ProductGroupDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ProductGroupDto>> Handle(ListProductGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await repository.GetAllAsync(cancellationToken);
        return [.. groups.Select(ProductGroupDto.FromDomain)];
    }
}
