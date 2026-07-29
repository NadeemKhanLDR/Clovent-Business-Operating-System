using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.EndOfDay.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using MediatR;

namespace Clovent.Restaurant.Application.EndOfDay.Queries;

/// <summary>
/// Computes the Day-End / Z-report for one warehouse on one calendar day
/// (UTC) - Today's Sales, Cash Collected, Items Sold (ordered by quantity,
/// doubling as Top Selling Items), Cash Summary (by payment method),
/// Receipt Count, Transaction Summary (via <see cref="EndOfDayReportDto.VoidedOrderCount"/>),
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
/// exact.
/// </remarks>
public sealed record GetEndOfDayReportQuery(Guid WarehouseId, DateOnly Date) : IRequest<EndOfDayReportDto>;

/// <summary>Handles <see cref="GetEndOfDayReportQuery"/>.</summary>
public sealed class GetEndOfDayReportQueryHandler(
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IPaymentRepository paymentRepository,
    IPaymentMethodRepository paymentMethodRepository) : IRequestHandler<GetEndOfDayReportQuery, EndOfDayReportDto>
{
    private const string CashMethodName = "Cash";

    /// <inheritdoc/>
    public async Task<EndOfDayReportDto> Handle(GetEndOfDayReportQuery request, CancellationToken cancellationToken)
    {
        var warehouseId = new WarehouseId(request.WarehouseId);
        var allOrders = await orderRepository.GetAllAsync(cancellationToken);

        bool MatchesDay(Order order) =>
            order.WarehouseId == warehouseId && DateOnly.FromDateTime(order.UpdatedAtUtc.UtcDateTime) == request.Date;

        var completedToday = allOrders.Where(o => o.Status == OrderStatus.Completed && MatchesDay(o)).ToList();
        var voidedTodayCount = allOrders.Count(o => o.Status == OrderStatus.Voided && MatchesDay(o));

        var paymentMethodNames = (await paymentMethodRepository.GetAllAsync(cancellationToken))
            .ToDictionary(m => m.Id, m => m.Name.Value);

        decimal totalSales = 0m;
        decimal cashCollected = 0m;
        var itemTotals = new Dictionary<Clovent.Catalog.Variants.ProductVariantId, (decimal Quantity, decimal Total)>();
        var methodTotals = new Dictionary<string, decimal>();

        foreach (var order in completedToday)
        {
            var lines = await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            foreach (var line in lines.Where(l => !l.IsVoided))
            {
                var (quantity, total) = itemTotals.GetValueOrDefault(line.ProductVariantId);
                itemTotals[line.ProductVariantId] = (quantity + line.Quantity, total + line.LineTotal);
            }

            var payments = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            foreach (var payment in payments.Where(p => !p.IsVoided))
            {
                totalSales += payment.Amount;

                var methodName = paymentMethodNames.GetValueOrDefault(payment.PaymentMethodId, "(unknown)");
                methodTotals[methodName] = methodTotals.GetValueOrDefault(methodName) + payment.Amount;

                if (string.Equals(methodName, CashMethodName, StringComparison.OrdinalIgnoreCase))
                {
                    cashCollected += payment.Amount;
                }
            }
        }

        var itemsSold = itemTotals
            .Select(kvp => new EndOfDayItemSoldDto(kvp.Key.Value, kvp.Value.Quantity, kvp.Value.Total))
            .OrderByDescending(i => i.Quantity)
            .ToList();

        var cashSummary = methodTotals
            .Select(kvp => new EndOfDayPaymentMethodTotalDto(kvp.Key, kvp.Value))
            .OrderByDescending(m => m.Total)
            .ToList();

        var receiptCount = completedToday.Count;

        return new EndOfDayReportDto(
            request.WarehouseId,
            request.Date,
            totalSales,
            cashCollected,
            itemsSold,
            cashSummary,
            receiptCount,
            voidedTodayCount,
            receiptCount > 0 ? totalSales / receiptCount : 0m);
    }
}
