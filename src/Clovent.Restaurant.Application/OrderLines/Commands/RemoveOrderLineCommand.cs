using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Commands;

/// <summary>
/// Removes a mistakenly-added line from an order: detaches it from the
/// order's tracked line list (<see cref="Order.RemoveOrderLine"/>) and
/// voids it (<see cref="OrderLine.Void"/>) so it is excluded
/// from totals - the line record itself is kept, never deleted, for audit
/// history.
/// </summary>
public sealed record RemoveOrderLineCommand(Guid OrderId, Guid OrderLineId) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="RemoveOrderLineCommand"/>.</summary>
public sealed class RemoveOrderLineCommandHandler(IOrderRepository orderRepository, IOrderLineRepository orderLineRepository)
    : IRequestHandler<RemoveOrderLineCommand, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(RemoveOrderLineCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var lineId = new OrderLineId(request.OrderLineId);
        var line = await orderLineRepository.GetByIdAsync(lineId, cancellationToken)
            ?? throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        order.RemoveOrderLine(lineId);
        if (!line.IsVoided)
        {
            line.Void();
        }

        return OrderLineDto.FromDomain(line);
    }
}
