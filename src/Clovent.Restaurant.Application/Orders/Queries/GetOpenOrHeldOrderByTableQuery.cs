using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Queries;

/// <summary>Retrieves the open or held order currently seated at a table, if any - the POS screen's "select a table" data source.</summary>
public sealed record GetOpenOrHeldOrderByTableQuery(Guid TableId) : IRequest<OrderDto?>;

/// <summary>Handles <see cref="GetOpenOrHeldOrderByTableQuery"/>.</summary>
public sealed class GetOpenOrHeldOrderByTableQueryHandler(IOrderRepository repository)
    : IRequestHandler<GetOpenOrHeldOrderByTableQuery, OrderDto?>
{
    /// <inheritdoc/>
    public async Task<OrderDto?> Handle(GetOpenOrHeldOrderByTableQuery request, CancellationToken cancellationToken)
    {
        var orders = await repository.GetOpenOrHeldByTableIdAsync(new TableId(request.TableId), cancellationToken);
        return orders.Select(OrderDto.FromDomain).FirstOrDefault();
    }
}
