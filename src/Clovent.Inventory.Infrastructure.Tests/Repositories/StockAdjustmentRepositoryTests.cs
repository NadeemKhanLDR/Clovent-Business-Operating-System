using Clovent.Catalog.Variants;
using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Infrastructure.Repositories;
using Clovent.Inventory.Infrastructure.Tests.TestSupport;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Infrastructure.Tests.Repositories;

public class StockAdjustmentRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var adjustment = StockAdjustment.Create(warehouseId, variantId, StockAdjustmentType.Increase, 15, "Physical count discrepancy");

        await using (var writeContext = CreateContext())
        {
            var repository = new StockAdjustmentRepository(writeContext);
            await repository.AddAsync(adjustment);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new StockAdjustmentRepository(readContext).GetByIdAsync(adjustment.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(warehouseId, reloaded!.WarehouseId);
        Assert.Equal(variantId, reloaded.ProductVariantId);
        Assert.Equal(StockAdjustmentType.Increase, reloaded.AdjustmentType);
        Assert.Equal(15, reloaded.Quantity);
        Assert.Equal("Physical count discrepancy", reloaded.Reason);
        Assert.Equal(StockAdjustmentStatus.Pending, reloaded.Status);
    }

    [Fact]
    public async Task GetByWarehouseIdAsync_FiltersToOwningWarehouse()
    {
        var warehouseId = WarehouseId.New();
        var otherWarehouseId = WarehouseId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new StockAdjustmentRepository(writeContext);
            await repository.AddAsync(StockAdjustment.Create(warehouseId, ProductVariantId.New(), StockAdjustmentType.Increase, 5, "Reason A"));
            await repository.AddAsync(StockAdjustment.Create(warehouseId, ProductVariantId.New(), StockAdjustmentType.Decrease, 3, "Reason B"));
            await repository.AddAsync(StockAdjustment.Create(otherWarehouseId, ProductVariantId.New(), StockAdjustmentType.Increase, 8, "Reason C"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new StockAdjustmentRepository(readContext).GetByWarehouseIdAsync(warehouseId);

        Assert.Equal(2, found.Count);
        Assert.All(found, a => Assert.Equal(warehouseId, a.WarehouseId));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryAdjustment()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new StockAdjustmentRepository(writeContext);
            await repository.AddAsync(StockAdjustment.Create(WarehouseId.New(), ProductVariantId.New(), StockAdjustmentType.Increase, 5, "Reason A"));
            await repository.AddAsync(StockAdjustment.Create(WarehouseId.New(), ProductVariantId.New(), StockAdjustmentType.Decrease, 3, "Reason B"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new StockAdjustmentRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new StockAdjustmentRepository(context).GetByIdAsync(StockAdjustmentId.New());

        Assert.Null(result);
    }
}
