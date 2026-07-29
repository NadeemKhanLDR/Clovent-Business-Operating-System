using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Moves a dine-in order to a different table - vacates the old table and seats the new one.</summary>
public sealed record TransferOrderTableCommand(Guid OrderId, Guid NewTableId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="TransferOrderTableCommand"/>.</summary>
public sealed class TransferOrderTableCommandHandler(IOrderRepository orderRepository, ITableRepository tableRepository)
    : IRequestHandler<TransferOrderTableCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(TransferOrderTableCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var newTableId = new TableId(request.NewTableId);
        var newTable = await tableRepository.GetByIdAsync(newTableId, cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.NewTableId);

        var oldTableId = order.TableId;

        order.AssignTable(newTableId);
        newTable.Occupy();

        if (oldTableId is { } previousTableId && previousTableId != newTableId)
        {
            var oldTable = await tableRepository.GetByIdAsync(previousTableId, cancellationToken);
            oldTable?.Vacate();
        }

        return OrderDto.FromDomain(order);
    }
}
