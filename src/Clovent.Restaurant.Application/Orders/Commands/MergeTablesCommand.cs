using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>
/// Merges one table's open/held order into another's (e.g. two tables
/// pushed together for one party): every order line moves to the target
/// order (creating one there if the target table has none open yet), the
/// source order is cancelled (now empty), and the source table is vacated.
/// See <c>OrderLifecycle.md</c>.
/// </summary>
public sealed record MergeTablesCommand(Guid SourceTableId, Guid TargetTableId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="MergeTablesCommand"/>.</summary>
public sealed class MergeTablesCommandHandler(
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    ITableRepository tableRepository,
    IOrderNumberSequenceRepository? orderNumberSequenceRepository = null)
    : IRequestHandler<MergeTablesCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(MergeTablesCommand request, CancellationToken cancellationToken)
    {
        var sourceTableId = new TableId(request.SourceTableId);
        var targetTableId = new TableId(request.TargetTableId);

        if (sourceTableId == targetTableId)
            throw RestaurantDomainException.CannotMergeTableIntoItself(sourceTableId);

        var sourceOrders = await orderRepository.GetOpenOrHeldByTableIdAsync(sourceTableId, cancellationToken);
        var sourceOrder = sourceOrders.FirstOrDefault()
            ?? throw RestaurantDomainException.TableHasNoOpenOrder(sourceTableId);

        if (sourceOrder.PaymentIds.Count > 0)
            throw RestaurantDomainException.OrderHasPayments(sourceOrder.Id);

        var targetTable = await tableRepository.GetByIdAsync(targetTableId, cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TargetTableId);

        var targetOrders = await orderRepository.GetOpenOrHeldByTableIdAsync(targetTableId, cancellationToken);
        var targetOrder = targetOrders.FirstOrDefault();
        if (targetOrder is null)
        {
            OrderNumber orderNumber;
            if (orderNumberSequenceRepository is not null)
            {
                var sequence = await orderNumberSequenceRepository.GetSingletonAsync(cancellationToken);
                if (sequence is null)
                {
                    sequence = OrderNumberSequence.CreateDefault();
                    await orderNumberSequenceRepository.AddAsync(sequence, cancellationToken);
                }
                orderNumber = sequence.Next();
            }
            else
            {
                orderNumber = OrderNumber.Generate(DateTimeOffset.UtcNow);
            }

            targetOrder = Order.Create(OrderType.DineIn, sourceOrder.WarehouseId, targetTableId, orderNumber);
            await orderRepository.AddAsync(targetOrder, cancellationToken);
            targetTable.Occupy();
        }
        else if (targetOrder.Status == OrderStatus.Held)
        {
            targetOrder.Resume();
        }

        if (sourceOrder.Status == OrderStatus.Held)
        {
            sourceOrder.Resume();
        }

        foreach (var lineId in sourceOrder.OrderLineIds.ToList())
        {
            var line = await orderLineRepository.GetByIdAsync(lineId, cancellationToken)
                ?? throw new NotFoundException(nameof(OrderLine), lineId.Value);

            sourceOrder.RemoveOrderLine(lineId);
            line.TransferToOrder(targetOrder.Id);
            targetOrder.AddOrderLine(lineId);
        }

        sourceOrder.Cancel($"Merged into table {request.TargetTableId}");

        var sourceTable = await tableRepository.GetByIdAsync(sourceTableId, cancellationToken);
        sourceTable?.Vacate();

        return OrderDto.FromDomain(targetOrder);
    }
}
