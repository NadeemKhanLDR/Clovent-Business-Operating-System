using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Commands;

/// <summary>Voids an order line - excludes it from totals and future kitchen tickets without removing it from the order.</summary>
public sealed record VoidOrderLineCommand(Guid OrderLineId) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="VoidOrderLineCommand"/>.</summary>
public sealed class VoidOrderLineCommandHandler(
    IOrderLineRepository repository,
    IOrderRepository? orderRepository = null,
    IKitchenTicketRepository? kitchenTicketRepository = null) : IRequestHandler<VoidOrderLineCommand, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(VoidOrderLineCommand request, CancellationToken cancellationToken)
    {
        var line = await repository.GetByIdAsync(new OrderLineId(request.OrderLineId), cancellationToken)
            ?? throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        if (orderRepository is not null)
        {
            var order = await orderRepository.GetByIdAsync(line.OrderId, cancellationToken);
            if (order is not null && order.Status != OrderStatus.Open)
                throw RestaurantDomainException.OrderNotOpen(order.Id, order.Status);
        }

        line.Void();

        if (kitchenTicketRepository is not null)
        {
            var tickets = await kitchenTicketRepository.GetByOrderIdAsync(line.OrderId, cancellationToken);
            var activeLineIds = (await repository.GetByOrderIdAsync(line.OrderId, cancellationToken))
                .Where(l => !l.IsVoided && l.Id != line.Id)
                .Select(l => l.Id)
                .ToHashSet();

            foreach (var ticket in tickets.Where(t =>
                         t.Status is not (KitchenTicketStatus.Served or KitchenTicketStatus.Cancelled) &&
                         t.OrderLineIds.Contains(line.Id) &&
                         !t.OrderLineIds.Any(activeLineIds.Contains)))
            {
                ticket.Cancel();
            }
        }

        return OrderLineDto.FromDomain(line);
    }
}
