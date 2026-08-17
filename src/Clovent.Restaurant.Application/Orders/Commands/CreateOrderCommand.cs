using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Opens a new order. For <see cref="OrderType.DineIn"/>, also seats the given table.</summary>
public sealed record CreateOrderCommand(OrderType OrderType, Guid WarehouseId, Guid? TableId = null) : IRequest<OrderDto>;

/// <summary>Handles <see cref="CreateOrderCommand"/>.</summary>
public sealed class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    ITableRepository tableRepository,
    IOrderNumberSequenceRepository orderNumberSequenceRepository)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var tableId = request.TableId is { } rawTableId ? new TableId(rawTableId) : (TableId?)null;

        if (tableId is { } requestedTableId)
        {
            await RequireTableHasNoOpenOrderAsync(requestedTableId, cancellationToken);
        }

        // Get-or-create-then-advance, same shape as CompleteOrderCommandHandler's
        // DailySalesSequence usage - the one sequence row is created with
        // defaults on first use, then every call issues and persists the
        // next number as part of this same unit of work.
        var sequence = await orderNumberSequenceRepository.GetSingletonAsync(cancellationToken);
        if (sequence is null)
        {
            sequence = OrderNumberSequence.CreateDefault();
            await orderNumberSequenceRepository.AddAsync(sequence, cancellationToken);
        }

        var order = Order.Create(request.OrderType, new WarehouseId(request.WarehouseId), tableId, sequence.Next());

        if (tableId is { } seatedTableId)
        {
            var table = await tableRepository.GetByIdAsync(seatedTableId, cancellationToken)
                ?? throw new NotFoundException(nameof(Table), seatedTableId.Value);

            table.Occupy();
        }

        await orderRepository.AddAsync(order, cancellationToken);

        return OrderDto.FromDomain(order);
    }

    /// <summary>
    /// Refuses a second dine-in order for a table that already has one open or
    /// held, checked against the orders themselves rather than the table's
    /// occupancy flag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this is not already covered by <see cref="Table.Occupy"/>:</b>
    /// that method throws for a table which is not Available/Reserved, so it
    /// does block the common case - but only as a side effect of a
    /// <em>separate</em> field that is free to drift out of step with the
    /// orders. Production data shows the drift is real: table T-01 sits
    /// Occupied with no open or held order at all. Whenever occupancy drifts
    /// the other way - Available while an order is still open - the occupancy
    /// check waves the second order straight through and the table ends up
    /// with two live bills. The orders table is the authority on whether a
    /// table has a bill; this asks it directly.
    /// </para>
    /// <para>
    /// <b>Deliberately not relying on UI state:</b> the POS screen disables New
    /// Dine-In whenever an order is loaded (<c>UpdateButtonStates</c>), but
    /// that is one screen's button, not an invariant - a second terminal, a
    /// replayed command or any future caller bypasses it entirely.
    /// </para>
    /// <para>
    /// <b>Concurrency - what this does and does not guarantee.</b> This is
    /// check-then-act, and the persistence model offers nothing to make it
    /// atomic: <c>Orders</c> carries no concurrency token and there is no
    /// unique index over (TableId, Status). Two terminals issuing
    /// CreateOrderCommand for the same free table at the same moment can both
    /// pass this check and both insert. The window is narrow and the check
    /// closes the far likelier drift and replay paths, but it is
    /// <b>not</b> a concurrency guarantee, and it is not claimed as one. Making
    /// it one needs a filtered unique index in the database - a migration,
    /// deliberately not created here.
    /// </para>
    /// <para>
    /// Take-away orders never reach this: they carry no table, and the caller
    /// only invokes it when a table id is present.
    /// </para>
    /// </remarks>
    /// <exception cref="RestaurantDomainException">The table already has an open or held order.</exception>
    private async Task RequireTableHasNoOpenOrderAsync(TableId tableId, CancellationToken cancellationToken)
    {
        var existing = await orderRepository.GetOpenOrHeldByTableIdAsync(tableId, cancellationToken);
        if (existing.FirstOrDefault() is { } occupyingOrder)
        {
            throw RestaurantDomainException.TableAlreadyHasOpenOrder(tableId, occupyingOrder.OrderNumber);
        }
    }
}
