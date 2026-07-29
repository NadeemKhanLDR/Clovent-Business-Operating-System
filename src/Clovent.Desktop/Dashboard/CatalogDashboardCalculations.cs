using Clovent.Inventory.Application.WarehouseStocks.Dtos;

namespace Clovent.Desktop.Dashboard;

/// <summary>
/// Pure calculation logic for the Milestone 14 ("Product Catalog &amp;
/// Inventory Foundation") dashboard widgets (Low Stock, Out of Stock,
/// Inventory Value), extracted so it can be unit tested without a Windows
/// Forms message loop - the same reasoning already applied to
/// <c>Clovent.Desktop.MasterData.MasterDataFilter</c>.
/// </summary>
public static class CatalogDashboardCalculations
{
    /// <summary>
    /// Counts balances that are low but not yet exhausted: a minimum stock
    /// level has actually been set (<c>0</c> means "no policy"), there is
    /// still some quantity on hand, and it has fallen to or below that
    /// minimum.
    /// </summary>
    public static int CountLowStock(IEnumerable<WarehouseStockDto> stocks) =>
        stocks.Count(s => s.MinimumStock > 0 && s.QuantityOnHand > 0 && s.QuantityOnHand <= s.MinimumStock);

    /// <summary>Counts balances with zero or negative quantity on hand (negative only possible under an <see cref="WarehouseStockDto.AllowNegativeStock"/> policy).</summary>
    public static int CountOutOfStock(IEnumerable<WarehouseStockDto> stocks) =>
        stocks.Count(s => s.QuantityOnHand <= 0);

    /// <summary>
    /// Total inventory value: each balance's quantity on hand times its
    /// variant's unit cost, summed. <paramref name="unitCostSelector"/>
    /// resolves a variant's current cost price (<c>0</c> for a variant with
    /// no cost price recorded yet, contributing nothing rather than
    /// throwing - an incomplete catalog is expected mid-setup, not an
    /// error).
    /// </summary>
    public static decimal CalculateInventoryValue(IEnumerable<WarehouseStockDto> stocks, Func<Guid, decimal> unitCostSelector) =>
        stocks.Sum(s => s.QuantityOnHand * unitCostSelector(s.ProductVariantId));
}
