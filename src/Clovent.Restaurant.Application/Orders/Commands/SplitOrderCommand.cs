using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>
/// Splits a subset of one dine-in order's lines off into a new order at a
/// different table (e.g. a large party asking for separate checks by
/// table). The moved lines' <see cref="OrderLine.OrderId"/> changes; the
/// source order keeps whichever lines were not selected. See
/// <c>OrderLifecycle.md</c>.
/// </summary>
public sealed record SplitOrderCommand(Guid SourceOrderId, IReadOnlyList<Guid> OrderLineIds, Guid TargetTableId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="SplitOrderCommand"/>.</summary>
public sealed class SplitOrderCommandHandler(IOrderRepository orderRepository, IOrderLineRepository orderLineRepository, ITableRepository tableRepository)
    : IRequestHandler<SplitOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(SplitOrderCommand request, CancellationToken cancellationToken)
    {
        var sourceOrderId = new OrderId(request.SourceOrderId);
        var sourceOrder = await orderRepository.GetByIdAsync(sourceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.SourceOrderId);

        var targetTableId = new TableId(request.TargetTableId);
        var targetTable = await tableRepository.GetByIdAsync(targetTableId, cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TargetTableId);

        var newOrder = Order.Create(OrderType.DineIn, sourceOrder.WarehouseId, targetTableId);
        targetTable.Occupy();

        foreach (var rawLineId in request.OrderLineIds)
        {
            var lineId = new OrderLineId(rawLineId);
            var line = await orderLineRepository.GetByIdAsync(lineId, cancellationToken)
                ?? throw new NotFoundException(nameof(OrderLine), rawLineId);

            if (line.OrderId != sourceOrderId)
                throw RestaurantDomainException.OrderLineNotOnOrder(sourceOrderId, lineId);

            sourceOrder.RemoveOrderLine(lineId);
            line.TransferToOrder(newOrder.Id);
            newOrder.AddOrderLine(lineId);
        }

        await orderRepository.AddAsync(newOrder, cancellationToken);

        return OrderDto.FromDomain(newOrder);
    }
}
