using Clovent.Restaurant.Application.RestaurantPulse.Dtos;

namespace Clovent.Restaurant.Application.RestaurantPulse;

/// <summary>
/// Pure calculator behind the Restaurant Pulse panel - the handler gathers
/// a day's aggregated samples and delegates every figure to this class, so
/// unit tests can feed synthetic data with no database at all. Deterministic
/// arithmetic only; empty input yields zeros and nulls, never exceptions.
/// </summary>
public static class RestaurantPulseCalculator
{
    /// <summary>Service-time samples longer than two hours are treated as data anomalies and excluded from the average.</summary>
    public static readonly TimeSpan MaxPlausibleServiceTime = TimeSpan.FromHours(2);

    /// <summary>
    /// Computes the panel from today's completed-order samples, today's
    /// beverage samples (unit prices sold today), and yesterday's sales
    /// total.
   /// </summary>
    /// <param name="todayOrders">Today's completed orders, aggregated as samples.</param>
    /// <param name="yesterdaySales">Yesterday's total sales; a zero makes the vs-yesterday percentage null (undefined), not infinite.</param>
    /// <param name="beverageUnitPricesToday">Today's observed beverage unit prices - their average prices the add-on opportunity. Pass empty (with <paramref name="beverageInfoAvailable"/> true) to make the opportunity null while still counting beverage-less orders.</param>
    /// <param name="beverageInfoAvailable">Whether category information was reliable enough to classify beverages at all.</param>
    public static RestaurantPulseDto Compute(
        IReadOnlyCollection<RestaurantPulseOrderSample> todayOrders,
        decimal yesterdaySales,
        IReadOnlyCollection<decimal> beverageUnitPricesToday,
        bool beverageInfoAvailable)
    {
        var todaySales = todayOrders.Sum(o => o.TotalAmount);
        var todayOrderCount = todayOrders.Count;
        var averageOrderValue = todayOrderCount > 0 ? todaySales / todayOrderCount : 0m;

        // Best seller by quantity; ties broken by name for determinism.
        var lines = todayOrders.SelectMany(o => o.Lines).ToList();
        var bestSeller = lines
            .GroupBy(l => l.VariantId)
            .Select(g => (Name: g.First().ProductName, Quantity: g.Sum(l => l.Quantity)))
            .OrderByDescending(x => x.Quantity)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .FirstOrDefault();
        var highestRevenue = lines
            .GroupBy(l => l.VariantId)
            .Select(g => (Name: g.First().ProductName, Amount: g.Sum(l => l.LineTotal)))
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .FirstOrDefault();

        decimal? salesVsYesterdayPercent = yesterdaySales > 0m
            ? Math.Round((todaySales - yesterdaySales) / yesterdaySales * 100m, 2)
            : null;

        // Average service time: (completed - created), excluding impossible values.
        var serviceTimes = todayOrders
            .Select(o => o.UpdatedAtUtc - o.CreatedAtUtc)
            .Where(elapsed => elapsed >= TimeSpan.Zero && elapsed <= MaxPlausibleServiceTime)
            .ToList();
        int? averageOrderTimeSeconds = serviceTimes.Count > 0
            ? (int)Math.Round(serviceTimes.Average(e => e.TotalSeconds))
            : null;

        var ordersWithoutBeverage = todayOrders.Count(o => !o.HasBeverageLine);
        decimal? beverageOpportunity = null;
        if (beverageInfoAvailable && beverageUnitPricesToday.Count > 0)
        {
            beverageOpportunity = Math.Round(ordersWithoutBeverage * beverageUnitPricesToday.Average(), 2);
        }

        return new RestaurantPulseDto(
            Math.Round(todaySales, 2),
            todayOrderCount,
            Math.Round(averageOrderValue, 2),
            bestSeller.Name is null ? null : bestSeller.Name,
            bestSeller.Name is null ? 0m : bestSeller.Quantity,
            highestRevenue.Name is null ? null : highestRevenue.Name,
            highestRevenue.Name is null ? 0m : Math.Round(highestRevenue.Amount, 2),
            salesVsYesterdayPercent,
            Math.Round(yesterdaySales, 2),
            averageOrderTimeSeconds,
            ordersWithoutBeverage,
            beverageOpportunity,
            beverageInfoAvailable,
            InventoryAvailable: false,
            LowStockItems: []);
    }
}
