using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Queries;

/// <summary>Retrieves every line currently belonging to an order.</summary>
public sealed record ListOrderLinesByOrderQuery(Guid OrderId) : IRequest<IReadOnlyCollection<OrderLineDto>>;

/// <summary>Handles <see cref="ListOrderLinesByOrderQuery"/>.</summary>
public sealed class ListOrderLinesByOrderQueryHandler(IOrderLineRepository repository)
    : IRequestHandler<ListOrderLinesByOrderQuery, IReadOnlyCollection<OrderLineDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<OrderLineDto>> Handle(ListOrderLinesByOrderQuery request, CancellationToken cancellationToken)
    {
        var lines = await repository.GetByOrderIdAsync(new OrderId(request.OrderId), cancellationToken);
        return [.. lines.Select(OrderLineDto.FromDomain)];
    }
}
