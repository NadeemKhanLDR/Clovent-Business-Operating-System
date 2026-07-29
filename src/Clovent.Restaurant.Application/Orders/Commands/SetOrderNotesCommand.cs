using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Sets an order's internal notes.</summary>
public sealed record SetOrderNotesCommand(Guid OrderId, string? Notes) : IRequest<OrderDto>;

/// <summary>Handles <see cref="SetOrderNotesCommand"/>.</summary>
public sealed class SetOrderNotesCommandHandler(IOrderRepository repository) : IRequestHandler<SetOrderNotesCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(SetOrderNotesCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.SetNotes(request.Notes);
        return OrderDto.FromDomain(order);
    }
}
