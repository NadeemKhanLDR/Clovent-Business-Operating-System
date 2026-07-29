using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Queries;

/// <summary>Retrieves every table belonging to a dining area.</summary>
public sealed record ListTablesByDiningAreaQuery(Guid DiningAreaId) : IRequest<IReadOnlyCollection<TableDto>>;

/// <summary>Handles <see cref="ListTablesByDiningAreaQuery"/>.</summary>
public sealed class ListTablesByDiningAreaQueryHandler(ITableRepository repository)
    : IRequestHandler<ListTablesByDiningAreaQuery, IReadOnlyCollection<TableDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<TableDto>> Handle(ListTablesByDiningAreaQuery request, CancellationToken cancellationToken)
    {
        var tables = await repository.GetByDiningAreaIdAsync(new DiningAreaId(request.DiningAreaId), cancellationToken);
        return [.. tables.Select(TableDto.FromDomain)];
    }
}
