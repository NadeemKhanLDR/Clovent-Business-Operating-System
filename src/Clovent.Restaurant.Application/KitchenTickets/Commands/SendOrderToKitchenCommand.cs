using Clovent.Restaurant.Application.KitchenTickets.Dtos;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.KitchenTickets.Commands;

/// <summary>
/// Sends an order's currently active (non-voided) lines to the kitchen as a
/// new ticket - a snapshot of <see cref="OrderLine"/> ids at send-time (see
/// <see cref="KitchenTicket"/>'s doc comment). Lines added afterward need a
/// second ticket, not a mutation of this one.
/// </summary>
public sealed record SendOrderToKitchenCommand(Guid OrderId) : IRequest<KitchenTicketDto>;

/// <summary>Handles <see cref="SendOrderToKitchenCommand"/>.</summary>
public sealed class SendOrderToKitchenCommandHandler(IOrderRepository orderRepository, IOrderLineRepository orderLineRepository, IKitchenTicketRepository kitchenTicketRepository)
    : IRequestHandler<SendOrderToKitchenCommand, KitchenTicketDto>
{
    /// <inheritdoc/>
    public async Task<KitchenTicketDto> Handle(SendOrderToKitchenCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        _ = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var lines = await orderLineRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var activeLineIds = lines.Where(l => !l.IsVoided).Select(l => l.Id).ToList();

        var ticket = KitchenTicket.Create(orderId, activeLineIds);
        await kitchenTicketRepository.AddAsync(ticket, cancellationToken);

        return KitchenTicketDto.FromDomain(ticket);
    }
}
