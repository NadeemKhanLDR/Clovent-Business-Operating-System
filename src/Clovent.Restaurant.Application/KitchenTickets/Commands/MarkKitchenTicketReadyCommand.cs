using Clovent.Restaurant.Application.KitchenTickets.Dtos;
using Clovent.Restaurant.KitchenTickets;
using MediatR;

namespace Clovent.Restaurant.Application.KitchenTickets.Commands;

/// <summary>Marks a kitchen ticket ready to serve.</summary>
public sealed record MarkKitchenTicketReadyCommand(Guid KitchenTicketId) : IRequest<KitchenTicketDto>;

/// <summary>Handles <see cref="MarkKitchenTicketReadyCommand"/>.</summary>
public sealed class MarkKitchenTicketReadyCommandHandler(IKitchenTicketRepository repository) : IRequestHandler<MarkKitchenTicketReadyCommand, KitchenTicketDto>
{
    /// <inheritdoc/>
    public async Task<KitchenTicketDto> Handle(MarkKitchenTicketReadyCommand request, CancellationToken cancellationToken)
    {
        var ticket = await repository.GetByIdAsync(new KitchenTicketId(request.KitchenTicketId), cancellationToken)
            ?? throw new NotFoundException(nameof(KitchenTicket), request.KitchenTicketId);

        ticket.MarkReady();
        return KitchenTicketDto.FromDomain(ticket);
    }
}
