using Clovent.Restaurant.Application.KitchenTickets.Dtos;
using Clovent.Restaurant.KitchenTickets;
using MediatR;

namespace Clovent.Restaurant.Application.KitchenTickets.Commands;

/// <summary>Begins preparation of a kitchen ticket.</summary>
public sealed record StartKitchenTicketCommand(Guid KitchenTicketId) : IRequest<KitchenTicketDto>;

/// <summary>Handles <see cref="StartKitchenTicketCommand"/>.</summary>
public sealed class StartKitchenTicketCommandHandler(IKitchenTicketRepository repository) : IRequestHandler<StartKitchenTicketCommand, KitchenTicketDto>
{
    /// <inheritdoc/>
    public async Task<KitchenTicketDto> Handle(StartKitchenTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await repository.GetByIdAsync(new KitchenTicketId(request.KitchenTicketId), cancellationToken)
            ?? throw new NotFoundException(nameof(KitchenTicket), request.KitchenTicketId);

        ticket.Start();
        return KitchenTicketDto.FromDomain(ticket);
    }
}
