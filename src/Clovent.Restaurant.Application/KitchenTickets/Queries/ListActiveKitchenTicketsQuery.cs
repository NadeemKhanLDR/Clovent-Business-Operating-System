using Clovent.Restaurant.Application.KitchenTickets.Dtos;
using Clovent.Restaurant.KitchenTickets;
using MediatR;

namespace Clovent.Restaurant.Application.KitchenTickets.Queries;

/// <summary>Retrieves every ticket not yet served or cancelled - the Kitchen Ticket Viewer's data source.</summary>
public sealed record ListActiveKitchenTicketsQuery : IRequest<IReadOnlyCollection<KitchenTicketDto>>;

/// <summary>Handles <see cref="ListActiveKitchenTicketsQuery"/>.</summary>
public sealed class ListActiveKitchenTicketsQueryHandler(IKitchenTicketRepository repository)
    : IRequestHandler<ListActiveKitchenTicketsQuery, IReadOnlyCollection<KitchenTicketDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<KitchenTicketDto>> Handle(ListActiveKitchenTicketsQuery request, CancellationToken cancellationToken)
    {
        var tickets = await repository.GetActiveAsync(cancellationToken);
        return [.. tickets.Select(KitchenTicketDto.FromDomain)];
    }
}
