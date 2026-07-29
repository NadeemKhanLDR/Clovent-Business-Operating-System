using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Queries;

/// <summary>Retrieves every held order - the Hold Orders screen's data source.</summary>
public sealed record ListHeldOrdersQuery : IRequest<IReadOnlyCollection<OrderDto>>;

/// <summary>Handles <see cref="ListHeldOrdersQuery"/>.</summary>
public sealed class ListHeldOrdersQueryHandler(IOrderRepository repository) : IRequestHandler<ListHeldOrdersQuery, IReadOnlyCollection<OrderDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<OrderDto>> Handle(ListHeldOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await repository.GetHeldAsync(cancellationToken);
        return [.. orders.Select(OrderDto.FromDomain)];
    }
}
