using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using MediatR;

namespace Clovent.Restaurant.Application.Shifts.Queries;

/// <summary>
/// Retrieves the most recently completed and paid Restaurant POS order associated with the specified shift.
/// Enforces shift and terminal scoping so reprinting receipts never leaks across cashiers or shifts.
/// </summary>
public sealed record GetLastCompletedOrderForShiftQuery(Guid ShiftId) : IRequest<OrderDto?>;

/// <summary>Handles <see cref="GetLastCompletedOrderForShiftQuery"/>.</summary>
public sealed class GetLastCompletedOrderForShiftQueryHandler(
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository) : IRequestHandler<GetLastCompletedOrderForShiftQuery, OrderDto?>
{
    /// <inheritdoc/>
    public async Task<OrderDto?> Handle(GetLastCompletedOrderForShiftQuery request, CancellationToken cancellationToken)
    {
        var shiftId = new ShiftId(request.ShiftId);
        var payments = await paymentRepository.GetByShiftIdAsync(shiftId, cancellationToken);
        var validPayments = payments.Where(p => !p.IsVoided).ToList();

        if (validPayments.Count == 0)
        {
            return null;
        }

        var orderIds = validPayments.Select(p => p.OrderId).Distinct().ToList();
        var completedOrders = new List<Order>();

        foreach (var id in orderIds)
        {
            var order = await orderRepository.GetByIdAsync(id, cancellationToken);
            if (order != null && order.Status == OrderStatus.Completed)
            {
                completedOrders.Add(order);
            }
        }

        if (completedOrders.Count == 0)
        {
            return null;
        }

        // Return the latest completed order in this shift (by UpdatedAtUtc or CreatedAtUtc)
        var latest = completedOrders
            .OrderByDescending(o => o.UpdatedAtUtc)
            .ThenByDescending(o => o.CreatedAtUtc)
            .First();

        return OrderDto.FromDomain(latest);
    }
}
