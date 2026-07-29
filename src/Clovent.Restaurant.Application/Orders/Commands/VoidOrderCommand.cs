using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Voids an order. For a seated dine-in order, also vacates its table.</summary>
public sealed record VoidOrderCommand(Guid OrderId, string Reason) : IRequest<OrderDto>;

/// <summary>Handles <see cref="VoidOrderCommand"/>.</summary>
public sealed class VoidOrderCommandHandler(IOrderRepository orderRepository, ITableRepository tableRepository)
    : IRequestHandler<VoidOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(VoidOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.Void(request.Reason);

        if (order.TableId is { } tableId)
        {
            var table = await tableRepository.GetByIdAsync(tableId, cancellationToken);
            table?.Vacate();
        }

        return OrderDto.FromDomain(order);
    }
}
