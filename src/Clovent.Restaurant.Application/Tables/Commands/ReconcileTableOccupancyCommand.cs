using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>
/// Reconciles table occupancy against actual active Dine-In orders.
/// Tables marked Occupied with no active (Open/Held) Dine-In orders are vacated.
/// Tables marked Available that have an active (Open/Held) Dine-In order are occupied.
/// Historical (Cancelled, Completed, Voided) orders do not keep a table occupied.
/// </summary>
public sealed record ReconcileTableOccupancyCommand : IRequest<Unit>;

/// <summary>Handles <see cref="ReconcileTableOccupancyCommand"/>.</summary>
public sealed class ReconcileTableOccupancyCommandHandler(
    ITableRepository tableRepository,
    IOrderRepository orderRepository) : IRequestHandler<ReconcileTableOccupancyCommand, Unit>
{
    /// <inheritdoc/>
    public async Task<Unit> Handle(ReconcileTableOccupancyCommand request, CancellationToken cancellationToken)
    {
        var tables = await tableRepository.GetAllAsync(cancellationToken);
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

        return Unit.Value;
    }
}
