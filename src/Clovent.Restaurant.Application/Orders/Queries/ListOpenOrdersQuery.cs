using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Queries;

/// <summary>Retrieves every open order - the Running Orders screen's data source.</summary>
public sealed record ListOpenOrdersQuery : IRequest<IReadOnlyCollection<OrderDto>>;

/// <summary>Handles <see cref="ListOpenOrdersQuery"/>.</summary>
public sealed class ListOpenOrdersQueryHandler(IOrderRepository repository) : IRequestHandler<ListOpenOrdersQuery, IReadOnlyCollection<OrderDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<OrderDto>> Handle(ListOpenOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await repository.GetOpenAsync(cancellationToken);
        return [.. orders.Select(OrderDto.FromDomain)];
    }
}
