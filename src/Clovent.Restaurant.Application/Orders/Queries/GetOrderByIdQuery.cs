using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Queries;

/// <summary>Retrieves an order by id.</summary>
public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="GetOrderByIdQuery"/>.</summary>
public sealed class GetOrderByIdQueryHandler(IOrderRepository repository) : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        return OrderDto.FromDomain(order);
    }
}
