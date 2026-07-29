using Clovent.Identity.Branches;
using Clovent.Restaurant.Application.DiningAreas.Dtos;
using Clovent.Restaurant.DiningAreas;
using MediatR;

namespace Clovent.Restaurant.Application.DiningAreas.Queries;

/// <summary>Retrieves every dining area belonging to a branch.</summary>
public sealed record ListDiningAreasByBranchQuery(Guid BranchId) : IRequest<IReadOnlyCollection<DiningAreaDto>>;

/// <summary>Handles <see cref="ListDiningAreasByBranchQuery"/>.</summary>
public sealed class ListDiningAreasByBranchQueryHandler(IDiningAreaRepository repository)
    : IRequestHandler<ListDiningAreasByBranchQuery, IReadOnlyCollection<DiningAreaDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<DiningAreaDto>> Handle(ListDiningAreasByBranchQuery request, CancellationToken cancellationToken)
    {
        var areas = await repository.GetByBranchIdAsync(new BranchId(request.BranchId), cancellationToken);
        return [.. areas.Select(DiningAreaDto.FromDomain)];
    }
}
