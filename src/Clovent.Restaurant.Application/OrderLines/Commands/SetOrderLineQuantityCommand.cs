using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Commands;

/// <summary>Changes an order line's quantity.</summary>
public sealed record SetOrderLineQuantityCommand(Guid OrderLineId, decimal Quantity) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="SetOrderLineQuantityCommand"/>.</summary>
public sealed class SetOrderLineQuantityCommandHandler(
    IOrderLineRepository repository,
    IOrderRepository? orderRepository = null) : IRequestHandler<SetOrderLineQuantityCommand, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(SetOrderLineQuantityCommand request, CancellationToken cancellationToken)
    {
        var line = await repository.GetByIdAsync(new OrderLineId(request.OrderLineId), cancellationToken)
            ?? throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        if (orderRepository is not null)
        {
            var order = await orderRepository.GetByIdAsync(line.OrderId, cancellationToken);
            if (order is not null && order.Status != OrderStatus.Open)
                throw RestaurantDomainException.OrderNotOpen(order.Id, order.Status);
        }

        line.SetQuantity(request.Quantity);
        return OrderLineDto.FromDomain(line);
    }
}
