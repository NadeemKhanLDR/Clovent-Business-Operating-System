using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Commands;

/// <summary>Restores a voided order line.</summary>
public sealed record UnvoidOrderLineCommand(Guid OrderLineId) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="UnvoidOrderLineCommand"/>.</summary>
public sealed class UnvoidOrderLineCommandHandler(
    IOrderLineRepository repository,
    IOrderRepository? orderRepository = null) : IRequestHandler<UnvoidOrderLineCommand, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(UnvoidOrderLineCommand request, CancellationToken cancellationToken)
    {
        var line = await repository.GetByIdAsync(new OrderLineId(request.OrderLineId), cancellationToken)
            ?? throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        if (orderRepository is not null)
        {
            var order = await orderRepository.GetByIdAsync(line.OrderId, cancellationToken);
            if (order is not null && order.Status != OrderStatus.Open)
                throw RestaurantDomainException.OrderNotOpen(order.Id, order.Status);
        }

        line.Unvoid();
        return OrderLineDto.FromDomain(line);
    }
}
