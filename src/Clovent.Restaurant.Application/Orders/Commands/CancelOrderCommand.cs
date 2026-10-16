using Clovent.Platform.Execution;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Cancels an order before any payment. For a seated dine-in order, also vacates its table.</summary>
public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<OrderDto>, IRequireBranchScope;

/// <summary>Handles <see cref="CancelOrderCommand"/>.</summary>
public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    ITableRepository tableRepository,
    Clovent.Restaurant.Application.Security.IOrderScopeValidator? orderScopeValidator = null)
    : IRequestHandler<CancelOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (orderScopeValidator != null)
        {
            await orderScopeValidator.ValidateOrderAccessAsync(order, cancellationToken).ConfigureAwait(false);
        }

        order.Cancel(request.Reason);

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
