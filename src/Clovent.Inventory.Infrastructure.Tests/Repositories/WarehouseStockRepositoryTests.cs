using Clovent.Catalog.Variants;
using Clovent.Inventory.Infrastructure.Repositories;
using Clovent.Inventory.Infrastructure.Tests.TestSupport;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Infrastructure.Tests.Repositories;

public class WarehouseStockRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var stock = WarehouseStock.Create(warehouseId, variantId, minimumStock: 5, maximumStock: 100);
        stock.Receive(20);

        await using (var writeContext = CreateContext())
        {
            var repository = new WarehouseStockRepository(writeContext);
            await repository.AddAsync(stock);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new WarehouseStockRepository(readContext).GetByIdAsync(stock.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(warehouseId, reloaded!.WarehouseId);
        Assert.Equal(variantId, reloaded.ProductVariantId);
        Assert.Equal(20, reloaded.QuantityOnHand);
        Assert.Equal(5, reloaded.MinimumStock);
        Assert.Equal(100, reloaded.MaximumStock);
        Assert.False(reloaded.AllowNegativeStock);
    }

    [Fact]
    public async Task GetByWarehouseAndVariantAsync_FindsMatch()
    {
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var stock = WarehouseStock.Create(warehouseId, variantId);

        await using (var writeContext = CreateContext())
        {
            var repository = new WarehouseStockRepository(writeContext);
            await repository.AddAsync(stock);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new WarehouseStockRepository(readContext).GetByWarehouseAndVariantAsync(warehouseId, variantId);

        Assert.NotNull(found);
        Assert.Equal(stock.Id, found!.Id);
    }

    [Fact]
    public async Task GetByWarehouseIdAsync_FiltersToOwningWarehouse()
    {
        var warehouseId = WarehouseId.New();
        var otherWarehouseId = WarehouseId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new WarehouseStockRepository(writeContext);
            await repository.AddAsync(WarehouseStock.Create(warehouseId, ProductVariantId.New()));
            await repository.AddAsync(WarehouseStock.Create(warehouseId, ProductVariantId.New()));
            await repository.AddAsync(WarehouseStock.Create(otherWarehouseId, ProductVariantId.New()));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new WarehouseStockRepository(readContext).GetByWarehouseIdAsync(warehouseId);

        Assert.Equal(2, found.Count);
        Assert.All(found, s => Assert.Equal(warehouseId, s.WarehouseId));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new WarehouseStockRepository(context).GetByIdAsync(WarehouseStockId.New());

        Assert.Null(result);
    }
}
