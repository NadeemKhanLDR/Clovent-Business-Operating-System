using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Opens a new order. For <see cref="OrderType.DineIn"/>, also seats the given table.</summary>
public sealed record CreateOrderCommand(OrderType OrderType, Guid WarehouseId, Guid? TableId = null) : IRequest<OrderDto>;

/// <summary>Handles <see cref="CreateOrderCommand"/>.</summary>
public sealed class CreateOrderCommandHandler(IOrderRepository orderRepository, ITableRepository tableRepository)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var tableId = request.TableId is { } rawTableId ? new TableId(rawTableId) : (TableId?)null;
        var order = Order.Create(request.OrderType, new WarehouseId(request.WarehouseId), tableId);

        if (tableId is { } seatedTableId)
        {
            var table = await tableRepository.GetByIdAsync(seatedTableId, cancellationToken)
                ?? throw new NotFoundException(nameof(Table), seatedTableId.Value);

            table.Occupy();
        }

        await orderRepository.AddAsync(order, cancellationToken);

        return OrderDto.FromDomain(order);
    }
}
