using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Sets an order's customer-facing notes.</summary>
public sealed record SetOrderCustomerNotesCommand(Guid OrderId, string? CustomerNotes) : IRequest<OrderDto>;

/// <summary>Handles <see cref="SetOrderCustomerNotesCommand"/>.</summary>
public sealed class SetOrderCustomerNotesCommandHandler(IOrderRepository repository) : IRequestHandler<SetOrderCustomerNotesCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(SetOrderCustomerNotesCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.SetCustomerNotes(request.CustomerNotes);
        return OrderDto.FromDomain(order);
    }
}
