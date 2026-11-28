using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Cancels an order before any payment. For a seated dine-in order, also vacates its table.</summary>
public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<OrderDto>;

/// <summary>Handles <see cref="CancelOrderCommand"/>.</summary>
public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    ITableRepository tableRepository,
    IKitchenTicketRepository? kitchenTicketRepository = null)
    : IRequestHandler<CancelOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.Cancel(request.Reason);

        if (kitchenTicketRepository is not null)
        {
            var tickets = await kitchenTicketRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            foreach (var ticket in tickets.Where(t => t.Status is not (KitchenTicketStatus.Served or KitchenTicketStatus.Cancelled)))
            {
                ticket.Cancel();
            }
        }

        if (order.TableId is { } tableId)
        {
            var activeOrders = await orderRepository.GetOpenOrHeldByTableIdAsync(tableId, cancellationToken);
            if (!activeOrders.Any(o => o.Id != order.Id))
            {
                var table = await tableRepository.GetByIdAsync(tableId, cancellationToken);
                table?.Vacate();
            }
        }

        return OrderDto.FromDomain(order);
    }
}
