using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Refunds;
using MediatR;

namespace Clovent.Restaurant.Application.Reports;

/// <summary>One line item in a statutory tax breakdown report.</summary>
public sealed record TaxAuthorityReportItemDto(
    string Authority,
    string TaxCode,
    decimal RatePercentage,
    string TaxClassification,
    decimal TaxableBase,
    decimal TaxCharged,
    decimal TaxRefunded,
    decimal NetTax);

/// <summary>Aggregated response of the statutory Pakistan Sales Tax Summary report.</summary>
public sealed record TaxSummaryReportDto(
    Guid WarehouseId,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal GrossSalesBeforeDiscounts,
    decimal TotalDiscounts,
    decimal SalesExcludingTax,
    decimal TaxableSalesBase,
    decimal ZeroRatedSales,
    decimal ExemptSales,
    decimal OutOfScopeSales,
    decimal TotalTaxCharged,
    decimal TotalTaxRefunded,
    decimal NetTaxPayable,
    decimal NetSales,
    IReadOnlyList<TaxAuthorityReportItemDto> TaxBreakdown);

/// <summary>Query to produce the Pakistan Sales Tax Summary Report.</summary>
public sealed record GetTaxSummaryReportQuery(
    Guid WarehouseId,
    DateOnly FromDate,
    DateOnly ToDate) : IRequest<TaxSummaryReportDto>;

/// <summary>Handles <see cref="GetTaxSummaryReportQuery"/>.</summary>
public sealed class GetTaxSummaryReportQueryHandler(
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IRefundRepository refundRepository) : IRequestHandler<GetTaxSummaryReportQuery, TaxSummaryReportDto>
{
    /// <inheritdoc/>
    public async Task<TaxSummaryReportDto> Handle(GetTaxSummaryReportQuery request, CancellationToken cancellationToken)
    {
        var warehouseId = new WarehouseId(request.WarehouseId);
        var allOrders = await orderRepository.GetAllAsync(cancellationToken);

        bool MatchesDate(DateTimeOffset utc)
        {
            var d = DateOnly.FromDateTime(utc.UtcDateTime);
            return d >= request.FromDate && d <= request.ToDate;
        }

        var completedOrders = allOrders
            .Where(o => o.WarehouseId == warehouseId && o.Status == OrderStatus.Completed && MatchesDate(o.UpdatedAtUtc))
            .ToList();

        decimal grossSales = 0m;
        decimal totalDiscounts = 0m;
        decimal taxableBase = 0m;
        decimal zeroRatedSales = 0m;
        decimal exemptSales = 0m;
        decimal outOfScopeSales = 0m;
        decimal totalTaxCharged = 0m;

        var taxMap = new Dictionary<(string Authority, string Code, decimal Rate, string Classification), (decimal Base, decimal Charged, decimal Refunded)>();

        foreach (var order in completedOrders)
        {
            var lines = await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            foreach (var line in lines.Where(l => !l.IsVoided))
            {
                var gross = line.LineTotal;
                var disc = line.AllocatedDiscount;
                var net = gross - disc;

                grossSales += gross;
                totalDiscounts += disc;

                var lineBase = line.TaxableBase ?? net;
                var lineTax = line.TaxAmount ?? 0m;
                totalTaxCharged += lineTax;

                var key = (line.TaxAuthority ?? "PRA", line.TaxCode ?? $"PK-TAX-{line.TaxRatePercentage:0.##}", line.TaxRatePercentage, line.TaxClassification ?? "Taxable");
                var current = taxMap.GetValueOrDefault(key);
                taxMap[key] = (current.Base + lineBase, current.Charged + lineTax, current.Refunded);

                if (string.Equals(line.TaxClassification, "ZeroRated", StringComparison.OrdinalIgnoreCase))
                {
                    zeroRatedSales += net;
                }
                else if (string.Equals(line.TaxClassification, "Exempt", StringComparison.OrdinalIgnoreCase))
                {
                    exemptSales += net;
                }
                else if (string.Equals(line.TaxClassification, "OutOfScope", StringComparison.OrdinalIgnoreCase))
                {
                    outOfScopeSales += net;
                }
                else
                {
                    taxableBase += lineBase;
                }
            }
        }

        // Aggregate Refunds posted in this date range
        var allRefunds = await refundRepository.GetAllAsync(cancellationToken);
        var refundsInRange = allRefunds
            .Where(r => r.WarehouseId == warehouseId && MatchesDate(r.RefundedAtUtc))
            .ToList();

        decimal totalTaxRefunded = 0m;

        foreach (var r in refundsInRange)
        {
            totalTaxRefunded += r.TaxReversedTotal;
            foreach (var rl in r.Lines)
            {
                var key = ("PRA", rl.TaxCode, rl.TaxRatePercentage, rl.TaxClassification);
                var current = taxMap.GetValueOrDefault(key);
                taxMap[key] = (current.Base, current.Charged, current.Refunded + rl.TaxReversed);
            }
        }

        var breakdown = taxMap.Select(kv => new TaxAuthorityReportItemDto(
            kv.Key.Authority,
            kv.Key.Code,
            kv.Key.Rate,
            kv.Key.Classification,
            MoneyRoundingPolicy.RoundMoney(kv.Value.Base),
            MoneyRoundingPolicy.RoundMoney(kv.Value.Charged),
            MoneyRoundingPolicy.RoundMoney(kv.Value.Refunded),
            MoneyRoundingPolicy.RoundMoney(kv.Value.Charged - kv.Value.Refunded)))
            .OrderBy(b => b.TaxCode)
            .ToList();

        decimal salesExcludingTax = MoneyRoundingPolicy.RoundMoney(grossSales - totalDiscounts);
        decimal netTaxPayable = MoneyRoundingPolicy.RoundMoney(totalTaxCharged - totalTaxRefunded);
        decimal netSales = MoneyRoundingPolicy.RoundMoney(salesExcludingTax - refundsInRange.Sum(r => r.SubtotalRefunded - r.DiscountReversedTotal));

        return new TaxSummaryReportDto(
            request.WarehouseId,
            request.FromDate,
            request.ToDate,
            MoneyRoundingPolicy.RoundMoney(grossSales),
            MoneyRoundingPolicy.RoundMoney(totalDiscounts),
            salesExcludingTax,
            MoneyRoundingPolicy.RoundMoney(taxableBase),
            MoneyRoundingPolicy.RoundMoney(zeroRatedSales),
            MoneyRoundingPolicy.RoundMoney(exemptSales),
            MoneyRoundingPolicy.RoundMoney(outOfScopeSales),
            MoneyRoundingPolicy.RoundMoney(totalTaxCharged),
            MoneyRoundingPolicy.RoundMoney(totalTaxRefunded),
            netTaxPayable,
            netSales,
            breakdown);
    }
}
