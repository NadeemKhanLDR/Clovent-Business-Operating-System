using Clovent.Restaurant.Application.KitchenTickets.Dtos;
using Clovent.Restaurant.KitchenTickets;
using MediatR;

namespace Clovent.Restaurant.Application.KitchenTickets.Commands;

/// <summary>Cancels a kitchen ticket before it is served.</summary>
public sealed record CancelKitchenTicketCommand(Guid KitchenTicketId) : IRequest<KitchenTicketDto>;

/// <summary>Handles <see cref="CancelKitchenTicketCommand"/>.</summary>
public sealed class CancelKitchenTicketCommandHandler(IKitchenTicketRepository repository) : IRequestHandler<CancelKitchenTicketCommand, KitchenTicketDto>
{
    /// <inheritdoc/>
    public async Task<KitchenTicketDto> Handle(CancelKitchenTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await repository.GetByIdAsync(new KitchenTicketId(request.KitchenTicketId), cancellationToken)
            ?? throw new NotFoundException(nameof(KitchenTicket), request.KitchenTicketId);

        ticket.Cancel();
        return KitchenTicketDto.FromDomain(ticket);
    }
}
