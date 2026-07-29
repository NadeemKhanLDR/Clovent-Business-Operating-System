using Clovent.Catalog.Variants;
using Clovent.Inventory;
using Clovent.Inventory.WarehouseStocks;
using Clovent.Inventory.WarehouseStocks.Events;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Tests.WarehouseStocks;

public class WarehouseStockTests
{
    private static WarehouseStock CreateStock(decimal minimumStock = 0, decimal maximumStock = 0, bool allowNegativeStock = false) =>
        WarehouseStock.Create(WarehouseId.New(), ProductVariantId.New(), minimumStock, maximumStock, allowNegativeStock);

    [Fact]
    public void Create_Valid_ZeroBalances_RaisesWarehouseStockCreated()
    {
        var stock = CreateStock();

        Assert.Equal(0m, stock.QuantityOnHand);
        Assert.Equal(0m, stock.QuantityReserved);
        Assert.Equal(0m, stock.QuantityAvailable);
        Assert.IsType<WarehouseStockCreated>(Assert.Single(stock.DomainEvents));
    }

    [Fact]
    public void Create_MaximumLessThanMinimum_Throws()
    {
        Assert.Throws<InventoryDomainException>(() =>
            WarehouseStock.Create(WarehouseId.New(), ProductVariantId.New(), minimumStock: 10, maximumStock: 5));
    }

    [Fact]
    public void Receive_IncreasesQuantityOnHand_RaisesStockReceived()
    {
        var stock = CreateStock();
        stock.ClearDomainEvents();

        stock.Receive(100);

        Assert.Equal(100m, stock.QuantityOnHand);
        Assert.IsType<StockReceived>(Assert.Single(stock.DomainEvents));
    }

    [Fact]
    public void Issue_MoreThanOnHand_WithoutNegativePolicy_Throws()
    {
        var stock = CreateStock();
        stock.Receive(10);

        Assert.Throws<InventoryDomainException>(() => stock.Issue(20));
    }

    [Fact]
    public void Issue_MoreThanOnHand_WithNegativePolicy_Allowed()
    {
        var stock = CreateStock(allowNegativeStock: true);
        stock.Receive(10);

        stock.Issue(20);

        Assert.Equal(-10m, stock.QuantityOnHand);
    }

    [Fact]
    public void Reserve_MoreThanAvailable_Throws()
    {
        var stock = CreateStock();
        stock.Receive(10);

        Assert.Throws<InventoryDomainException>(() => stock.Reserve(20));
    }

    [Fact]
    public void Reserve_ThenRelease_RoundTrips()
    {
        var stock = CreateStock();
        stock.Receive(10);

        stock.Reserve(4);
        Assert.Equal(4m, stock.QuantityReserved);
        Assert.Equal(6m, stock.QuantityAvailable);

        stock.Release(4);
        Assert.Equal(0m, stock.QuantityReserved);
        Assert.Equal(10m, stock.QuantityAvailable);
    }

    [Fact]
    public void Release_MoreThanReserved_Throws()
    {
        var stock = CreateStock();
        stock.Receive(10);
        stock.Reserve(4);

        Assert.Throws<InventoryDomainException>(() => stock.Release(5));
    }

    [Fact]
    public void SetStockLevels_MaximumLessThanMinimum_Throws()
    {
        var stock = CreateStock();

        Assert.Throws<InventoryDomainException>(() => stock.SetStockLevels(10, 5));
    }

    [Fact]
    public void Receive_ZeroOrNegative_Throws()
    {
        var stock = CreateStock();

        Assert.Throws<ArgumentOutOfRangeException>(() => stock.Receive(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stock.Receive(-1));
    }
}
