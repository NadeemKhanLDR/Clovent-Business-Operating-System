using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Holds an order.</summary>
public sealed record HoldOrderCommand(Guid OrderId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="HoldOrderCommand"/>.</summary>
public sealed class HoldOrderCommandHandler(IOrderRepository repository) : IRequestHandler<HoldOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(HoldOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.Hold();
        return OrderDto.FromDomain(order);
    }
}
