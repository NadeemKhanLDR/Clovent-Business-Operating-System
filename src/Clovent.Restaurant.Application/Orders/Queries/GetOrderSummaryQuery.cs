using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Queries;

/// <summary>Computes an order's running total, tax summary, and payment balance - the POS screen's and Payment screen's data source.</summary>
public sealed record GetOrderSummaryQuery(Guid OrderId) : IRequest<OrderTotals>;

/// <summary>Handles <see cref="GetOrderSummaryQuery"/>.</summary>
public sealed class GetOrderSummaryQueryHandler(
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IDiscountRepository discountRepository,
    IServiceChargeRepository serviceChargeRepository,
    IPaymentRepository paymentRepository) : IRequestHandler<GetOrderSummaryQuery, OrderTotals>
{
    /// <inheritdoc/>
    public async Task<OrderTotals> Handle(GetOrderSummaryQuery request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        _ = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var lines = await orderLineRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var discounts = await discountRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var serviceCharges = await serviceChargeRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var payments = await paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);

        return OrderTotalsCalculator.Calculate(
            lines.Select(OrderLineDto.FromDomain).ToList(),
            discounts.Select(DiscountDto.FromDomain).ToList(),
            serviceCharges.Select(ServiceChargeDto.FromDomain).ToList(),
            payments.Select(PaymentDto.FromDomain).ToList());
    }
}
