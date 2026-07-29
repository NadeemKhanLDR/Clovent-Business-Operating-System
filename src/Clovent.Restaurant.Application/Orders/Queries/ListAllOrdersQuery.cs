using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Queries;

/// <summary>Retrieves every order, regardless of status.</summary>
public sealed record ListAllOrdersQuery : IRequest<IReadOnlyCollection<OrderDto>>;

/// <summary>Handles <see cref="ListAllOrdersQuery"/>.</summary>
public sealed class ListAllOrdersQueryHandler(IOrderRepository repository) : IRequestHandler<ListAllOrdersQuery, IReadOnlyCollection<OrderDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<OrderDto>> Handle(ListAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await repository.GetAllAsync(cancellationToken);
        return [.. orders.Select(OrderDto.FromDomain)];
    }
}
