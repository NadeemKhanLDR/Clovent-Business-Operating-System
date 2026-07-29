using Clovent.Restaurant.Application.KitchenTickets.Dtos;
using Clovent.Restaurant.KitchenTickets;
using MediatR;

namespace Clovent.Restaurant.Application.KitchenTickets.Commands;

/// <summary>Marks a kitchen ticket served.</summary>
public sealed record ServeKitchenTicketCommand(Guid KitchenTicketId) : IRequest<KitchenTicketDto>;

/// <summary>Handles <see cref="ServeKitchenTicketCommand"/>.</summary>
public sealed class ServeKitchenTicketCommandHandler(IKitchenTicketRepository repository) : IRequestHandler<ServeKitchenTicketCommand, KitchenTicketDto>
{
    /// <inheritdoc/>
    public async Task<KitchenTicketDto> Handle(ServeKitchenTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await repository.GetByIdAsync(new KitchenTicketId(request.KitchenTicketId), cancellationToken)
            ?? throw new NotFoundException(nameof(KitchenTicket), request.KitchenTicketId);

        ticket.Serve();
        return KitchenTicketDto.FromDomain(ticket);
    }
}
