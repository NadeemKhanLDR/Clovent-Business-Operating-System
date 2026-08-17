using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Frees a table.</summary>
public sealed record VacateTableCommand(Guid TableId) : IRequest<TableDto>;

/// <summary>Handles <see cref="VacateTableCommand"/>.</summary>
public sealed class VacateTableCommandHandler(ITableRepository repository, IOrderRepository orderRepository)
    : IRequestHandler<VacateTableCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(VacateTableCommand request, CancellationToken cancellationToken)
    {
        var tableId = new TableId(request.TableId);
        var table = await repository.GetByIdAsync(tableId, cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        await RequireNoLiveOrderAsync(tableId, cancellationToken);

        table.Vacate();
        return TableDto.FromDomain(table);
    }

    /// <summary>
    /// Refuses to free a table that still has an open or held order seated at
    /// it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Root cause this closes.</b> <see cref="Table.Vacate"/> refuses only
    /// an <see cref="TableOccupancyStatus.OutOfService"/> table - it cannot
    /// consult the orders, and the handler never did either. The Table
    /// Management screen enables its Vacate button for any Occupied table, so
    /// one click on a table with a live bill silently succeeded and left the
    /// order Open on a table reading Available. That is exactly what happened
    /// to <b>T-03 / ORD-54</b> in the development database: the floor plan
    /// showed the table free while a two-line bill was still running on it.
    /// It is the mirror image of the orphaned-occupancy problem (a table
    /// Occupied with no order) and comes from the same root - occupancy and
    /// orders being allowed to drift apart.
    /// </para>
    /// <para>
    /// <b>Why only the manual command is guarded.</b> The order-lifecycle
    /// handlers - Complete, Cancel, Void, Merge, Transfer - do not route
    /// through this command; they call <see cref="Table.Vacate"/> on the
    /// aggregate directly, as the closing step of an operation that is
    /// legitimately ending the order. Guarding here therefore leaves all of
    /// them untouched. It also has to be here rather than inside
    /// <see cref="Table.Vacate"/>: those handlers change the order's status
    /// in memory and save it in the same unit of work, so a repository query
    /// made mid-handler would still read the pre-change status from the
    /// database and reject a perfectly valid vacate.
    /// </para>
    /// </remarks>
    /// <exception cref="RestaurantDomainException">The table still has an open or held order.</exception>
    private async Task RequireNoLiveOrderAsync(TableId tableId, CancellationToken cancellationToken)
    {
        var liveOrders = await orderRepository.GetOpenOrHeldByTableIdAsync(tableId, cancellationToken);
        if (liveOrders.FirstOrDefault() is { } liveOrder)
        {
            throw RestaurantDomainException.TableHasLiveOrder(tableId, liveOrder.OrderNumber);
        }
    }
}
