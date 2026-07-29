using Clovent.Catalog.Variants;
using Clovent.Inventory.Infrastructure.Repositories;
using Clovent.Inventory.Infrastructure.Tests.TestSupport;
using Clovent.Inventory.Transactions;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Infrastructure.Tests.Repositories;

public class InventoryTransactionRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var transaction = InventoryTransaction.Create(warehouseId, variantId, InventoryTransactionType.Receipt, 10, "StockAdjustment", Guid.NewGuid(), "Initial receipt");

        await using (var writeContext = CreateContext())
        {
            var repository = new InventoryTransactionRepository(writeContext);
            await repository.AddAsync(transaction);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new InventoryTransactionRepository(readContext).GetByIdAsync(transaction.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(warehouseId, reloaded!.WarehouseId);
        Assert.Equal(variantId, reloaded.ProductVariantId);
        Assert.Equal(InventoryTransactionType.Receipt, reloaded.TransactionType);
        Assert.Equal(10, reloaded.Quantity);
        Assert.Equal("StockAdjustment", reloaded.ReferenceType);
        Assert.Equal("Initial receipt", reloaded.Notes);
    }

    [Fact]
    public async Task GetByWarehouseIdAsync_FiltersToOwningWarehouse()
    {
        var warehouseId = WarehouseId.New();
        var otherWarehouseId = WarehouseId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new InventoryTransactionRepository(writeContext);
            await repository.AddAsync(InventoryTransaction.Create(warehouseId, ProductVariantId.New(), InventoryTransactionType.Receipt, 5));
            await repository.AddAsync(InventoryTransaction.Create(warehouseId, ProductVariantId.New(), InventoryTransactionType.Issue, 3));
            await repository.AddAsync(InventoryTransaction.Create(otherWarehouseId, ProductVariantId.New(), InventoryTransactionType.Receipt, 8));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new InventoryTransactionRepository(readContext).GetByWarehouseIdAsync(warehouseId);

        Assert.Equal(2, found.Count);
        Assert.All(found, t => Assert.Equal(warehouseId, t.WarehouseId));
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsMostRecentFirst_LimitedToCount()
    {
        var warehouseId = WarehouseId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new InventoryTransactionRepository(writeContext);
            var now = DateTimeOffset.UtcNow;
            await repository.AddAsync(InventoryTransaction.Create(warehouseId, ProductVariantId.New(), InventoryTransactionType.Receipt, 1, occurredAtUtc: now.AddMinutes(-30)));
            await repository.AddAsync(InventoryTransaction.Create(warehouseId, ProductVariantId.New(), InventoryTransactionType.Receipt, 2, occurredAtUtc: now.AddMinutes(-10)));
            await repository.AddAsync(InventoryTransaction.Create(warehouseId, ProductVariantId.New(), InventoryTransactionType.Receipt, 3, occurredAtUtc: now));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var recent = await new InventoryTransactionRepository(readContext).GetRecentAsync(2);

        Assert.Equal(2, recent.Count);
        Assert.Equal(3, recent.First().Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new InventoryTransactionRepository(context).GetByIdAsync(InventoryTransactionId.New());

        Assert.Null(result);
    }
}
