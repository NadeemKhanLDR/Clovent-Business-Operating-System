using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Tables.Dtos;

namespace Clovent.Desktop.Dashboard;

/// <summary>
/// Pure calculation logic for the restaurant dashboard widgets
/// (Open Tables, Today's Sales, Top Selling Items, Delivery Orders, Credit Metrics),
/// extracted so it can be unit tested without a Windows Forms message loop -
/// the same reasoning already applied to <see cref="CatalogDashboardCalculations"/>.
/// </summary>
public static class RestaurantDashboardCalculations
{
    /// <summary>Counts tables currently seated with an open order.</summary>
    public static int CountOccupiedTables(IEnumerable<TableDto> tables) =>
        tables.Count(t => t.OccupancyStatus == "Occupied");

    /// <summary>Filters to orders completed on the given date - the "Today's Sales" widget's population.</summary>
    public static IReadOnlyList<OrderDto> FilterCompletedOn(IEnumerable<OrderDto> orders, DateOnly date) =>
        [.. orders.Where(o => o.Status == "Completed" && DateOnly.FromDateTime(o.UpdatedAtUtc.UtcDateTime) == date)];

    /// <summary>
    /// Ranks product variants by total quantity sold across the given active
    /// (non-voided) order lines, highest first - the "Top Selling Items"
    /// widget's data.
    /// </summary>
    public static IReadOnlyList<(Guid ProductVariantId, decimal Quantity)> TopSellingItems(IEnumerable<OrderLineDto> lines, int top = 5) =>
        [.. lines
            .Where(l => !l.IsVoided)
            .GroupBy(l => l.ProductVariantId)
            .Select(g => (ProductVariantId: g.Key, Quantity: g.Sum(l => l.Quantity)))
            .OrderByDescending(x => x.Quantity)
            .Take(top)];

    /// <summary>Counts delivery orders placed on the given date.</summary>
    public static int CountDeliveryOrdersOn(IEnumerable<OrderDto> orders, DateOnly date) =>
        orders.Count(o => o.OrderType == "Delivery" && DateOnly.FromDateTime(o.CreatedAtUtc.UtcDateTime) == date);

    /// <summary>Counts customers with a positive receivable balance.</summary>
    public static int CountCustomersWithBalance(IEnumerable<CustomerReceivableRowDto> rows) =>
        rows.Count(r => r.Receivable > 0);
}
