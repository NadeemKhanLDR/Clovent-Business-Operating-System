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
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.Status is not (OrderStatus.Open or OrderStatus.Held))
            throw RestaurantDomainException.OrderNotOpen(order.Id, order.Status);

        var lines = await orderLineRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var existingTickets = await kitchenTicketRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var alreadySentLineIds = existingTickets
            .Where(t => t.Status != KitchenTicketStatus.Cancelled)
            .SelectMany(t => t.OrderLineIds)
            .ToHashSet();

        var activeLineIds = lines
            .Where(l => !l.IsVoided && !alreadySentLineIds.Contains(l.Id))
            .Select(l => l.Id)
            .ToList();

        if (activeLineIds.Count == 0)
            throw RestaurantDomainException.NoUnsentKitchenLines(orderId);

        var ticket = KitchenTicket.Create(orderId, activeLineIds);
        await kitchenTicketRepository.AddAsync(ticket, cancellationToken);

        return KitchenTicketDto.FromDomain(ticket);
    }
}
