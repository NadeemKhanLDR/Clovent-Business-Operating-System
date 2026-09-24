using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Queries;

/// <summary>Retrieves every table across every dining area - the floor-plan/POS table grid's data source.</summary>
public sealed record ListAllTablesQuery : IRequest<IReadOnlyCollection<TableDto>>;

/// <summary>Handles <see cref="ListAllTablesQuery"/>.</summary>
public sealed class ListAllTablesQueryHandler(ITableRepository repository, IOrderRepository? orderRepository = null)
    : IRequestHandler<ListAllTablesQuery, IReadOnlyCollection<TableDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<TableDto>> Handle(ListAllTablesQuery request, CancellationToken cancellationToken)
    {
        var tables = await repository.GetAllAsync(cancellationToken);

        if (orderRepository is not null)
        {
            var activeTableIds = await orderRepository.GetActiveTableIdsAsync(cancellationToken);
            foreach (var table in tables)
            {
                var hasActiveOrder = activeTableIds.Contains(table.Id);
                if (table.OccupancyStatus == TableOccupancyStatus.Occupied && !hasActiveOrder)
                {
                    table.Vacate();
                }
                else if (table.OccupancyStatus == TableOccupancyStatus.Available && hasActiveOrder)
                {
                    table.Occupy();
                }
            }
        }

        return [.. tables.Select(TableDto.FromDomain)];
    }
}
