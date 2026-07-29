using Clovent.Restaurant.Application.DiningAreas.Dtos;
using Clovent.Restaurant.DiningAreas;
using MediatR;

namespace Clovent.Restaurant.Application.DiningAreas.Queries;

/// <summary>Retrieves every dining area across every branch - the Table Management scoping picker's data source.</summary>
public sealed record ListAllDiningAreasQuery : IRequest<IReadOnlyCollection<DiningAreaDto>>;

/// <summary>Handles <see cref="ListAllDiningAreasQuery"/>.</summary>
public sealed class ListAllDiningAreasQueryHandler(IDiningAreaRepository repository)
    : IRequestHandler<ListAllDiningAreasQuery, IReadOnlyCollection<DiningAreaDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<DiningAreaDto>> Handle(ListAllDiningAreasQuery request, CancellationToken cancellationToken)
    {
        var areas = await repository.GetAllAsync(cancellationToken);
        return [.. areas.Select(DiningAreaDto.FromDomain)];
    }
}
