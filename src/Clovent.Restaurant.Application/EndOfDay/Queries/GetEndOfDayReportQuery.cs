using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.EndOfDay.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using MediatR;

namespace Clovent.Restaurant.Application.EndOfDay.Queries;

/// <summary>
/// Computes the Day-End / Z-report (a.k.a. Daily Sales Report) for one
/// warehouse over a calendar-day range (UTC, inclusive both ends - a single
/// day is simply <c>FromDate == ToDate</c>) - Today's Sales, Cash Collected,
/// Items Sold (ordered by quantity, doubling as Top Selling Items), Cash
/// Summary (by payment method), Bills (one row per completed order), Receipt
/// Count, Transaction Summary (via <see cref="EndOfDayReportDto.VoidedOrderCount"/>),
/// and Average Sale. Inventory Movement and Stock Remaining are not part of
/// this DTO - the Desktop report screen composes those directly from
/// <c>Clovent.Inventory.Application</c>'s existing
/// <c>ListInventoryTransactionsByWarehouseQuery</c>/<c>ListWarehouseStocksByWarehouseQuery</c>
/// rather than this query re-wrapping data another query already exposes.
/// </summary>
/// <remarks>
/// Walks every order via <see cref="IOrderRepository.GetAllAsync"/> and
/// filters in memory - the same "no batched read model yet" limitation
/// <c>Dashboard.md</c> already documents for Today's Sales/Top Selling/
/// Inventory Value; acceptable at this MVP's demo scale, a real concern once
/// order volume grows. <see cref="EndOfDayReportDto.CashCollected"/> and
/// <see cref="EndOfDayReportDto.CashSummary"/> match a payment's method by
/// name ("Cash", case-insensitive) since <c>PaymentMethod</c> has no typed
/// Cash/Card distinction - a fragile string match, documented here rather
/// than silently assumed; a future milestone should add a
/// <c>PaymentMethodKind</c> enum if cash-drawer reconciliation needs to be
/// exact. <see cref="EndOfDayReportDto.CardCollected"/> is the same kind of
/// fragile match, this time on the substring "card" (so "Credit Card",
/// "Debit Card", or a plain "Card" method all count) - it exists purely so
/// the Restaurant "Sales Summary" screen can show a Cash/Card pair of
/// figures without asking a restaurant owner to open the underlying Cash
/// Summary breakdown by payment method.
/// </remarks>
public sealed record GetEndOfDayReportQuery(Guid WarehouseId, DateOnly FromDate, DateOnly ToDate) : IRequest<EndOfDayReportDto>;

/// <summary>Handles <see cref="GetEndOfDayReportQuery"/>.</summary>
public sealed class GetEndOfDayReportQueryHandler(
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IPaymentRepository paymentRepository,
    IPaymentMethodRepository paymentMethodRepository) : IRequestHandler<GetEndOfDayReportQuery, EndOfDayReportDto>
{
    private const string CashMethodName = "Cash";
    private const string CardMethodNameFragment = "card";

    /// <inheritdoc/>
    public async Task<EndOfDayReportDto> Handle(GetEndOfDayReportQuery request, CancellationToken cancellationToken)
    {
        var warehouseId = new WarehouseId(request.WarehouseId);
        var allOrders = await orderRepository.GetAllAsync(cancellationToken);

        bool MatchesRange(Order order)
        {
            if (order.WarehouseId != warehouseId)
            {
                return false;
            }

            var orderDate = DateOnly.FromDateTime(order.UpdatedAtUtc.UtcDateTime);
            return orderDate >= request.FromDate && orderDate <= request.ToDate;
        }

        var completedInRange = allOrders.Where(o => o.Status == OrderStatus.Completed && MatchesRange(o)).ToList();
        var voidedInRangeCount = allOrders.Count(o => o.Status == OrderStatus.Voided && MatchesRange(o));

        var paymentMethodNames = (await paymentMethodRepository.GetAllAsync(cancellationToken))
            .ToDictionary(m => m.Id, m => m.Name.Value);

        decimal totalSales = 0m;
        decimal cashCollected = 0m;
        decimal cardCollected = 0m;
        var itemTotals = new Dictionary<Clovent.Catalog.Variants.ProductVariantId, (decimal Quantity, decimal Total)>();
        var methodTotals = new Dictionary<string, decimal>();
        var bills = new List<EndOfDayBillDto>();

        foreach (var order in completedInRange)
        {
            var lines = await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            foreach (var line in lines.Where(l => !l.IsVoided))
            {
                var (quantity, total) = itemTotals.GetValueOrDefault(line.ProductVariantId);
                itemTotals[line.ProductVariantId] = (quantity + line.Quantity, total + line.LineTotal);
            }

            var payments = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            var activePayments = payments.Where(p => !p.IsVoided).ToList();
            decimal orderTotal = 0m;
            var methodNamesForOrder = new List<string>();

            foreach (var payment in activePayments)
            {
                totalSales += payment.Amount;
                orderTotal += payment.Amount;

                var methodName = paymentMethodNames.GetValueOrDefault(payment.PaymentMethodId, "(unknown)");
                methodTotals[methodName] = methodTotals.GetValueOrDefault(methodName) + payment.Amount;
                methodNamesForOrder.Add(methodName);

                if (string.Equals(methodName, CashMethodName, StringComparison.OrdinalIgnoreCase))
                {
                    cashCollected += payment.Amount;
                }
                else if (methodName.Contains(CardMethodNameFragment, StringComparison.OrdinalIgnoreCase))
                {
                    cardCollected += payment.Amount;
                }
            }

            bills.Add(new EndOfDayBillDto(
                order.Id.Value,
                order.OrderNumber.Value,
                order.UpdatedAtUtc,
                orderTotal,
                methodNamesForOrder.Count > 0 ? string.Join(", ", methodNamesForOrder.Distinct()) : "(none)"));
        }

        var itemsSold = itemTotals
            .Select(kvp => new EndOfDayItemSoldDto(kvp.Key.Value, kvp.Value.Quantity, kvp.Value.Total))
            .OrderByDescending(i => i.Quantity)
            .ToList();

        var cashSummary = methodTotals
            .Select(kvp => new EndOfDayPaymentMethodTotalDto(kvp.Key, kvp.Value))
            .OrderByDescending(m => m.Total)
            .ToList();

        var receiptCount = completedInRange.Count;

        return new EndOfDayReportDto(
            request.WarehouseId,
            request.FromDate,
            request.ToDate,
            totalSales,
            cashCollected,
            cardCollected,
            itemsSold,
            cashSummary,
            [.. bills.OrderByDescending(b => b.CompletedAtUtc)],
            receiptCount,
            voidedInRangeCount,
            receiptCount > 0 ? totalSales / receiptCount : 0m);
    }
}
