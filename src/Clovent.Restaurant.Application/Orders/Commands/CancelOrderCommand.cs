using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Cancels an order before any payment. For a seated dine-in order, also vacates its table.</summary>
public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<OrderDto>;

/// <summary>Handles <see cref="CancelOrderCommand"/>.</summary>
public sealed class CancelOrderCommandHandler(IOrderRepository orderRepository, ITableRepository tableRepository)
    : IRequestHandler<CancelOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.Cancel(request.Reason);

        if (order.TableId is { } tableId)
        {
            var table = await tableRepository.GetByIdAsync(tableId, cancellationToken);
            table?.Vacate();
        }

        return OrderDto.FromDomain(order);
    }
}
