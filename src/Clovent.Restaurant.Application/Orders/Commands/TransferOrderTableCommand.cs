using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Moves a dine-in order to a different table - vacates the old table and seats the new one.</summary>
public sealed record TransferOrderTableCommand(Guid OrderId, Guid NewTableId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="TransferOrderTableCommand"/>.</summary>
/// <remarks>
/// <para>
/// The switch is atomic: every validation runs before any aggregate is mutated, and the
/// unit-of-work behavior only commits when the handler completes, so a rejected switch
/// leaves the order, both tables and their occupancy flags exactly as they were.
/// </para>
/// <para>
/// <b>Why the orders are checked rather than the new table's occupancy flag:</b> that flag
/// is free to drift (see <c>CreateOrderCommandHandler</c>); the orders are the authority on
/// whether a table has a live bill. A drifted <c>Occupied</c> flag on the new table is
/// self-healed by the <c>Occupy</c> call below once no order claims the table.
/// </para>
/// <para>
/// The old table is only vacated when no <em>other</em> open or held order still seats at
/// it - the same guard <c>CancelOrderCommandHandler</c> uses - so a transfer can never
/// free a table another live bill depends on.
/// </para>
/// </remarks>
public sealed class TransferOrderTableCommandHandler(IOrderRepository orderRepository, ITableRepository tableRepository)
    : IRequestHandler<TransferOrderTableCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(TransferOrderTableCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var newTableId = new TableId(request.NewTableId);
        var newTable = await tableRepository.GetByIdAsync(newTableId, cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.NewTableId);

        // Reject before mutating anything when another live order already seats at the
        // target table. TableAlreadyHasOpenOrder's message tells the cashier to settle or
        // move that order first - exactly the "Table T-02 is currently occupied." refusal.
        var ordersAtTarget = await orderRepository.GetOpenOrHeldByTableIdAsync(newTableId, cancellationToken);
        if (ordersAtTarget.Any(o => o.Id != order.Id))
        {
            throw RestaurantDomainException.TableAlreadyHasOpenOrder(newTableId, ordersAtTarget.First(o => o.Id != order.Id).OrderNumber);
        }

        var oldTableId = order.TableId;

        // Throws when the order is take-away or no longer Open/Held, and when the new
        // table cannot be seated (Reserved drift is tolerated by Occupy; OutOfService is
        // refused). All in-memory, before the unit of work commits anything.
        order.AssignTable(newTableId);
        newTable.Occupy();

        if (oldTableId is { } previousTableId && previousTableId != newTableId)
        {
            var remainingOrders = await orderRepository.GetOpenOrHeldByTableIdAsync(previousTableId, cancellationToken);
            if (!remainingOrders.Any(o => o.Id != order.Id))
            {
                var oldTable = await tableRepository.GetByIdAsync(previousTableId, cancellationToken);
                oldTable?.Vacate();
            }
        }

        return OrderDto.FromDomain(order);
    }
}
