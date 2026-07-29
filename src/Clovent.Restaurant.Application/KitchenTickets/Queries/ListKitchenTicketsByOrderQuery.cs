using Clovent.Restaurant.Application.KitchenTickets.Dtos;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.KitchenTickets.Queries;

/// <summary>Retrieves every ticket sent for an order.</summary>
public sealed record ListKitchenTicketsByOrderQuery(Guid OrderId) : IRequest<IReadOnlyCollection<KitchenTicketDto>>;

/// <summary>Handles <see cref="ListKitchenTicketsByOrderQuery"/>.</summary>
public sealed class ListKitchenTicketsByOrderQueryHandler(IKitchenTicketRepository repository)
    : IRequestHandler<ListKitchenTicketsByOrderQuery, IReadOnlyCollection<KitchenTicketDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<KitchenTicketDto>> Handle(ListKitchenTicketsByOrderQuery request, CancellationToken cancellationToken)
    {
        var tickets = await repository.GetByOrderIdAsync(new OrderId(request.OrderId), cancellationToken);
        return [.. tickets.Select(KitchenTicketDto.FromDomain)];
    }
}
