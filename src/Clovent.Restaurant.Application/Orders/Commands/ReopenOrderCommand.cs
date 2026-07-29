using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Reopens a voided or cancelled order. For a dine-in order, re-seats its table.</summary>
public sealed record ReopenOrderCommand(Guid OrderId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="ReopenOrderCommand"/>.</summary>
public sealed class ReopenOrderCommandHandler(IOrderRepository orderRepository, ITableRepository tableRepository)
    : IRequestHandler<ReopenOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(ReopenOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.Reopen();

        if (order.TableId is { } tableId)
        {
            var table = await tableRepository.GetByIdAsync(tableId, cancellationToken);
            if (table is not null && table.OccupancyStatus != TableOccupancyStatus.Occupied)
            {
                table.Occupy();
            }
        }

        return OrderDto.FromDomain(order);
    }
}
