using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Queries;

/// <summary>Retrieves every table across every dining area - the floor-plan/POS table grid's data source.</summary>
public sealed record ListAllTablesQuery : IRequest<IReadOnlyCollection<TableDto>>;

/// <summary>Handles <see cref="ListAllTablesQuery"/>.</summary>
public sealed class ListAllTablesQueryHandler(ITableRepository repository)
    : IRequestHandler<ListAllTablesQuery, IReadOnlyCollection<TableDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<TableDto>> Handle(ListAllTablesQuery request, CancellationToken cancellationToken)
    {
        var tables = await repository.GetAllAsync(cancellationToken);
        return [.. tables.Select(TableDto.FromDomain)];
    }
}
