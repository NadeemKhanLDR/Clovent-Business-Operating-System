using Clovent.Restaurant.Application.KitchenTickets.Dtos;
using Clovent.Restaurant.KitchenTickets;
using MediatR;

namespace Clovent.Restaurant.Application.KitchenTickets.Queries;

/// <summary>Retrieves a kitchen ticket by id.</summary>
public sealed record GetKitchenTicketByIdQuery(Guid KitchenTicketId) : IRequest<KitchenTicketDto>;

/// <summary>Handles <see cref="GetKitchenTicketByIdQuery"/>.</summary>
public sealed class GetKitchenTicketByIdQueryHandler(IKitchenTicketRepository repository) : IRequestHandler<GetKitchenTicketByIdQuery, KitchenTicketDto>
{
    /// <inheritdoc/>
    public async Task<KitchenTicketDto> Handle(GetKitchenTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await repository.GetByIdAsync(new KitchenTicketId(request.KitchenTicketId), cancellationToken)
            ?? throw new NotFoundException(nameof(KitchenTicket), request.KitchenTicketId);

        return KitchenTicketDto.FromDomain(ticket);
    }
}
