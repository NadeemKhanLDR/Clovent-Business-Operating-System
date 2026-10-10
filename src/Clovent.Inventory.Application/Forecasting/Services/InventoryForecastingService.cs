using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.Forecasting.Dtos;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using Microsoft.Extensions.Logging;

namespace Clovent.Inventory.Application.Forecasting.Services;

/// <summary>
/// Forecasting engine implementing velocity-based consumption modeling,
/// rolling hourly history analysis, day-of-week seasonality, and GRN-synchronized depletion projection.
/// </summary>
public sealed class InventoryForecastingService(
    IWarehouseStockRepository warehouseStockRepository,
    IInventoryTransactionRepository transactionRepository,
    ILogger<InventoryForecastingService> logger) : IInventoryForecastingService
{
    private const decimal MinSeasonality = 0.2m;
    private const decimal MaxSeasonality = 3.0m;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InventoryBurnRateDto>> CalculateBurnRatesAndDepletionAsync(
        Guid warehouseId,
        DateTimeOffset? nextScheduledGrnUtc = null,
        int observationHours = 168,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddHours(-Math.Max(24, observationHours));
        var targetWhId = new WarehouseId(warehouseId);

        var stocks = await warehouseStockRepository.GetByWarehouseIdAsync(targetWhId, cancellationToken);
        var transactions = await transactionRepository.GetByWarehouseIdAsync(targetWhId, cancellationToken);

        // Filter transactions for issues in the observation window
        var windowIssues = transactions
            .Where(t => t.TransactionType == InventoryTransactionType.Issue &&
                        t.OccurredAtUtc >= windowStart &&
                        t.OccurredAtUtc <= now)
            .ToList();

        var effectiveGrnTime = nextScheduledGrnUtc ?? now.AddHours(24);
        var hoursUntilGrn = (decimal)Math.Max(0.0, (effectiveGrnTime - now).TotalHours);

        var results = new List<InventoryBurnRateDto>();

        foreach (var stock in stocks)
        {
            var variantIssues = windowIssues
                .Where(t => t.ProductVariantId == stock.ProductVariantId)
                .ToList();

            var totalIssued = variantIssues.Sum(t => t.Quantity);

            // Compute elapsed observation span
            var earliestIssue = variantIssues.Count > 0 ? variantIssues.Min(t => t.OccurredAtUtc) : windowStart;
            var observedSpanHours = (decimal)Math.Max(1.0, (now - earliestIssue).TotalHours);
            var baseHourlyBurnRate = totalIssued > 0 ? Math.Round(totalIssued / observedSpanHours, 4) : 0m;

            // Day-of-week seasonality analysis
            var seasonality = ComputeDayOfWeekSeasonality(variantIssues, baseHourlyBurnRate, windowStart, now);

            // Available stock
            var available = Math.Max(0m, stock.QuantityOnHand - stock.QuantityReserved);

            // Forward simulation
            var (hoursUntilDepletion, projectedDepletionUtc, demand24h, projectedConsumptionUntilGrn) =
                SimulateDepletion(now, available, baseHourlyBurnRate, seasonality, effectiveGrnTime);

            bool isDepletedBeforeGrn = available <= 0m || hoursUntilDepletion < hoursUntilGrn;
            decimal projectedDeficitAtGrn = Math.Max(0m, projectedConsumptionUntilGrn - available);

            // Calculate reorder recommendation
            decimal buffer = Math.Max(stock.MinimumStock, baseHourlyBurnRate * 12.0m);
            decimal recommendedReorder = projectedDeficitAtGrn + buffer;
            if (stock.MaximumStock > 0 && available + recommendedReorder > stock.MaximumStock)
            {
                recommendedReorder = Math.Max(0m, stock.MaximumStock - available);
            }
            recommendedReorder = Math.Round(recommendedReorder, 2);

            string urgency = DetermineUrgency(available, hoursUntilDepletion, isDepletedBeforeGrn, projectedConsumptionUntilGrn, stock.MinimumStock);

            var skuDisplay = $"VAR-{stock.ProductVariantId.Value.ToString()[..8].ToUpperInvariant()}";
            var itemName = $"Inventory Item ({skuDisplay})";

            results.Add(new InventoryBurnRateDto(
                stock.ProductVariantId.Value,
                skuDisplay,
                itemName,
                warehouseId,
                stock.QuantityOnHand,
                stock.QuantityReserved,
                available,
                stock.MinimumStock,
                stock.MaximumStock,
                baseHourlyBurnRate,
                seasonality,
                hoursUntilDepletion,
                projectedDepletionUtc,
                effectiveGrnTime,
                Math.Round(hoursUntilGrn, 1),
                isDepletedBeforeGrn,
                Math.Round(projectDeficit(projectedDeficitAtGrn), 2),
                recommendedReorder,
                urgency,
                demand24h));
        }

        logger.LogInformation("Calculated inventory burn rates for warehouse {WarehouseId}: {Count} items evaluated.", warehouseId, results.Count);

        return results.OrderByDescending(r => r.IsDepletedBeforeGrn)
                      .ThenBy(r => r.HoursUntilDepletion)
                      .ToList();

        static decimal projectDeficit(decimal deficit) => deficit > 0 ? deficit : 0m;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StockoutAlertDto>> GenerateStockoutAlertsAsync(
        Guid warehouseId,
        DateTimeOffset? nextScheduledGrnUtc = null,
        int observationHours = 168,
        CancellationToken cancellationToken = default)
    {
        var burnRates = await CalculateBurnRatesAndDepletionAsync(
            warehouseId, nextScheduledGrnUtc, observationHours, cancellationToken);

        var alerts = new List<StockoutAlertDto>();

        foreach (var item in burnRates.Where(r => r.IsDepletedBeforeGrn || r.Urgency == "Critical" || r.Urgency == "High"))
        {
            var summary = item.QuantityAvailable <= 0m
                ? $"Stockout imminent: Item {item.Sku} is completely exhausted (0 available) with active burn rate {item.BaseHourlyBurnRate:F2}/hr."
                : $"Stockout predicted: Item {item.Sku} will deplete in {item.HoursUntilDepletion:F1} hours, before scheduled GRN in {item.HoursUntilGrn:F1} hours (Deficit: {item.ProjectedDeficitAtGrn:N2} units).";

            var severity = item.Urgency switch
            {
                "Critical" => "Critical",
                "High" => "High",
                _ => "Medium"
            };

            alerts.Add(new StockoutAlertDto(
                item.ProductVariantId,
                item.Sku,
                item.ItemName,
                item.WarehouseId,
                item.QuantityAvailable,
                item.MinimumStock,
                item.BaseHourlyBurnRate,
                item.HoursUntilDepletion,
                item.ProjectedDepletionUtc,
                item.NextScheduledGrnUtc,
                item.HoursUntilGrn,
                item.IsDepletedBeforeGrn,
                item.RecommendedReorderQuantity,
                severity,
                summary));
        }

        return alerts.OrderBy(a => a.HoursUntilDepletion).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReorderRecommendationDto>> GenerateReorderRecommendationsAsync(
        Guid warehouseId,
        DateTimeOffset? nextScheduledGrnUtc = null,
        int observationHours = 168,
        CancellationToken cancellationToken = default)
    {
        var burnRates = await CalculateBurnRatesAndDepletionAsync(
            warehouseId, nextScheduledGrnUtc, observationHours, cancellationToken);

        var recommendations = new List<ReorderRecommendationDto>();

        foreach (var item in burnRates.Where(r => r.RecommendedReorderQuantity > 0m))
        {
            var priority = item.Urgency switch
            {
                "Critical" => "Emergency",
                "High" => "High",
                _ => "Standard"
            };

            var orderBefore = item.ProjectedDepletionUtc.HasValue
                ? item.ProjectedDepletionUtc.Value.AddHours(-4) // Reorder 4 hours before stockout
                : DateTimeOffset.UtcNow.AddHours(12);

            if (orderBefore < DateTimeOffset.UtcNow)
                orderBefore = DateTimeOffset.UtcNow;

            var justification = item.IsDepletedBeforeGrn
                ? $"Projected stock exhaustion ({item.HoursUntilDepletion:F1}h remaining) prior to replenishment arrival ({item.HoursUntilGrn:F1}h)."
                : $"Replenishment required to maintain safety buffer ({item.MinimumStock:N2}) across next replenishment cycle.";

            recommendations.Add(new ReorderRecommendationDto(
                item.ProductVariantId,
                item.Sku,
                item.ItemName,
                item.WarehouseId,
                item.QuantityAvailable,
                item.RecommendedReorderQuantity,
                item.HoursUntilDepletion,
                orderBefore,
                item.NextScheduledGrnUtc,
                priority,
                justification));
        }

        return recommendations.OrderBy(r => r.ProjectedDepletionHours).ToList();
    }

    private static Dictionary<DayOfWeek, decimal> ComputeDayOfWeekSeasonality(
        IReadOnlyList<InventoryTransaction> issues,
        decimal baseRate,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd)
    {
        var result = new Dictionary<DayOfWeek, decimal>();
        var allDays = Enum.GetValues<DayOfWeek>();

        if (baseRate <= 0m || issues.Count == 0)
        {
            foreach (var d in allDays)
                result[d] = 1.0m;
            return result;
        }

        // Count observed hours for each day of week in the window
        var dayHours = new Dictionary<DayOfWeek, decimal>();
        var dayQuantities = new Dictionary<DayOfWeek, decimal>();
        foreach (var d in allDays)
        {
            dayHours[d] = 0m;
            dayQuantities[d] = 0m;
        }

        // Tally quantities
        foreach (var issue in issues)
        {
            var dow = issue.OccurredAtUtc.DayOfWeek;
            dayQuantities[dow] += issue.Quantity;
        }

        // Approximate hours per day in window
        var totalDays = (windowEnd - windowStart).TotalDays;
        var daysWithData = dayQuantities.Count(kv => kv.Value > 0);

        // If observation window is shorter than ~6 days or fewer than 3 days have data,
        // day-of-week seasonality cannot be reliably inferred; default to neutral 1.0m.
        if (totalDays < 6.0 || daysWithData <= 2)
        {
            foreach (var d in allDays)
                result[d] = 1.0m;
            return result;
        }

        var weeks = (decimal)(totalDays / 7.0);
        foreach (var d in allDays)
        {
            dayHours[d] = Math.Max(1.0m, weeks * 24.0m);
        }

        foreach (var d in allDays)
        {
            var q = dayQuantities[d];
            if (q == 0m)
            {
                result[d] = 1.0m;
                continue;
            }

            var h = dayHours[d];
            var dayRate = q / h;
            var factor = baseRate > 0 ? dayRate / baseRate : 1.0m;
            factor = Math.Clamp(factor, MinSeasonality, MaxSeasonality);
            result[d] = Math.Round(factor, 2);
        }

        return result;
    }

    private static (decimal HoursUntilDepletion, DateTimeOffset? ProjectedDepletionUtc, List<decimal> Demand24h, decimal ProjectedConsumptionUntilGrn)
        SimulateDepletion(
            DateTimeOffset now,
            decimal availableStock,
            decimal baseHourlyRate,
            Dictionary<DayOfWeek, decimal> seasonality,
            DateTimeOffset grnTime)
    {
        var demand24h = new List<decimal>();
        decimal consumptionUntilGrn = 0m;

        if (availableStock <= 0m && baseHourlyRate > 0m)
        {
            for (int i = 1; i <= 24; i++)
            {
                var fTime = now.AddHours(i);
                var s = seasonality.GetValueOrDefault(fTime.DayOfWeek, 1.0m);
                demand24h.Add(Math.Round(baseHourlyRate * s, 3));
            }
            return (0m, now, demand24h, baseHourlyRate * (decimal)(grnTime - now).TotalHours);
        }

        if (baseHourlyRate <= 0m)
        {
            for (int i = 1; i <= 24; i++) demand24h.Add(0m);
            return (9999.0m, null, demand24h, 0m);
        }

        decimal remaining = availableStock;
        decimal totalHours = 0m;
        bool depleted = false;

        var maxSimHours = 24 * 30; // 30 days max simulation
        for (int h = 1; h <= maxSimHours; h++)
        {
            var futureHour = now.AddHours(h);
            var dow = futureHour.DayOfWeek;
            var season = seasonality.GetValueOrDefault(dow, 1.0m);
            var hourlyDemand = baseHourlyRate * season;

            if (h <= 24)
            {
                demand24h.Add(Math.Round(hourlyDemand, 3));
            }

            if (futureHour <= grnTime)
            {
                consumptionUntilGrn += hourlyDemand;
            }

            if (!depleted)
            {
                if (remaining <= hourlyDemand)
                {
                    var fraction = hourlyDemand > 0 ? remaining / hourlyDemand : 0m;
                    totalHours = (h - 1) + fraction;
                    remaining = 0m;
                    depleted = true;
                }
                else
                {
                    remaining -= hourlyDemand;
                    totalHours = h;
                }
            }
        }

        if (!depleted)
        {
            totalHours = 9999.0m;
        }

        totalHours = Math.Round(totalHours, 1);
        DateTimeOffset? depletionTime = totalHours < 9999.0m ? now.AddHours((double)totalHours) : null;

        return (totalHours, depletionTime, demand24h, consumptionUntilGrn);
    }

    private static string DetermineUrgency(
        decimal available,
        decimal hoursUntilDepletion,
        bool isDepletedBeforeGrn,
        decimal consumptionUntilGrn,
        decimal minimumStock)
    {
        if (available <= 0m || hoursUntilDepletion <= 6m)
            return "Critical";

        if (isDepletedBeforeGrn)
            return "High";

        if (available - consumptionUntilGrn < minimumStock)
            return "Medium";

        return "Adequate";
    }
}
