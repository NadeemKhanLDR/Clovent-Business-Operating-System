using Clovent.Desktop.Dashboard;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Xunit;

namespace Clovent.Desktop.Tests.Dashboard;

public class CatalogDashboardCalculationsTests
{
    private static WarehouseStockDto CreateStock(decimal quantityOnHand, decimal minimumStock, Guid? variantId = null) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        variantId ?? Guid.NewGuid(),
        quantityOnHand,
        0,
        quantityOnHand,
        minimumStock,
        0,
        false,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow);

    [Fact]
    public void CountLowStock_QuantityAtOrBelowMinimum_Counts()
    {
        WarehouseStockDto[] stocks =
        [
            CreateStock(quantityOnHand: 5, minimumStock: 10),
            CreateStock(quantityOnHand: 10, minimumStock: 10),
            CreateStock(quantityOnHand: 50, minimumStock: 10),
        ];

        Assert.Equal(2, CatalogDashboardCalculations.CountLowStock(stocks));
    }

    [Fact]
    public void CountLowStock_NoMinimumStockSet_IsExcluded()
    {
        WarehouseStockDto[] stocks = [CreateStock(quantityOnHand: 1, minimumStock: 0)];

        Assert.Equal(0, CatalogDashboardCalculations.CountLowStock(stocks));
    }

    [Fact]
    public void CountLowStock_AlreadyOutOfStock_IsExcluded()
    {
        WarehouseStockDto[] stocks = [CreateStock(quantityOnHand: 0, minimumStock: 10)];

        Assert.Equal(0, CatalogDashboardCalculations.CountLowStock(stocks));
    }

    [Fact]
    public void CountOutOfStock_ZeroOrNegativeQuantity_Counts()
    {
        WarehouseStockDto[] stocks =
        [
            CreateStock(quantityOnHand: 0, minimumStock: 5),
            CreateStock(quantityOnHand: -3, minimumStock: 5),
            CreateStock(quantityOnHand: 10, minimumStock: 5),
        ];

        Assert.Equal(2, CatalogDashboardCalculations.CountOutOfStock(stocks));
    }

    [Fact]
    public void CalculateInventoryValue_SumsQuantityTimesUnitCost()
    {
        var variantA = Guid.NewGuid();
        var variantB = Guid.NewGuid();
        WarehouseStockDto[] stocks =
        [
            CreateStock(quantityOnHand: 10, minimumStock: 0, variantId: variantA),
            CreateStock(quantityOnHand: 4, minimumStock: 0, variantId: variantB),
        ];

        var costs = new Dictionary<Guid, decimal> { [variantA] = 2.5m, [variantB] = 10m };

        var result = CatalogDashboardCalculations.CalculateInventoryValue(stocks, id => costs.GetValueOrDefault(id));

        Assert.Equal(65m, result);
    }

    [Fact]
    public void CalculateInventoryValue_UnknownVariant_ContributesZero()
    {
        WarehouseStockDto[] stocks = [CreateStock(quantityOnHand: 10, minimumStock: 0)];

        var result = CatalogDashboardCalculations.CalculateInventoryValue(stocks, _ => 0m);

        Assert.Equal(0m, result);
    }
}
