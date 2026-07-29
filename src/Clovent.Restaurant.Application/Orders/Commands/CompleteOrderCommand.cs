using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>
/// Completes an order: verifies its balance is zero (the domain's own
/// <see cref="Order.Complete"/> deliberately does not - see its doc
/// comment), reduces warehouse stock for every active line by consuming
/// <c>Clovent.Inventory.Application</c>'s existing
/// <see cref="IssueStockCommand"/> rather than duplicating its
/// receive/issue/ledger logic, then closes the order and (for a seated
/// dine-in order) vacates its table.
/// </summary>
public sealed record CompleteOrderCommand(Guid OrderId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="CompleteOrderCommand"/>.</summary>
public sealed class CompleteOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IDiscountRepository discountRepository,
    IServiceChargeRepository serviceChargeRepository,
    IPaymentRepository paymentRepository,
    ITableRepository tableRepository,
    IMediator mediator) : IRequestHandler<CompleteOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var lines = await orderLineRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var discounts = await discountRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var serviceCharges = await serviceChargeRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var payments = await paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);

        var lineDtos = lines.Select(OrderLineDto.FromDomain).ToList();
        var totals = OrderTotalsCalculator.Calculate(
            lineDtos,
            discounts.Select(DiscountDto.FromDomain).ToList(),
            serviceCharges.Select(ServiceChargeDto.FromDomain).ToList(),
            payments.Select(PaymentDto.FromDomain).ToList());

        if (totals.Balance > 0.005m)
            throw RestaurantDomainException.OrderNotFullyPaid(orderId, totals.Balance);

        foreach (var line in lineDtos.Where(l => !l.IsVoided))
        {
            var stock = await mediator.Send(new GetWarehouseStockByWarehouseAndVariantQuery(order.WarehouseId.Value, line.ProductVariantId), cancellationToken);
            if (stock is not null)
            {
                await mediator.Send(new IssueStockCommand(stock.WarehouseStockId, line.Quantity, $"Order {order.OrderNumber.Value}"), cancellationToken);
            }
        }

        order.Complete();

        if (order.TableId is { } tableId)
        {
            var table = await tableRepository.GetByIdAsync(tableId, cancellationToken);
            table?.Vacate();
        }

        return OrderDto.FromDomain(order);
    }
}
