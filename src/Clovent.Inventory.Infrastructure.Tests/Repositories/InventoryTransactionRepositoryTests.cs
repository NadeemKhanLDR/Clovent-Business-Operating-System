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

    [Fact]
    public async Task ExecuteStockMovementAtomicallyAsync_DuplicateReference_RollsBackStockMutationAndRecovers()
    {
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var refundId = Guid.NewGuid();

        await using (var seedContext = CreateContext())
        {
            var stockRepo = new WarehouseStockRepository(seedContext);
            var txRepo = new InventoryTransactionRepository(seedContext);
            var stock = Clovent.Inventory.WarehouseStocks.WarehouseStock.Create(warehouseId, variantId);
            stock.Receive(20m);
            await stockRepo.AddAsync(stock);
            await txRepo.AddAsync(InventoryTransaction.Create(
                warehouseId,
                variantId,
                InventoryTransactionType.Receipt,
                2m,
                referenceType: "Refund",
                referenceId: refundId,
                notes: "Initial refund restock"));
            await seedContext.SaveChangesAsync();
        }

        await using var raceContext = CreateContext();
        var uow = new Clovent.Inventory.Infrastructure.Persistence.UnitOfWork(raceContext);
        var raceStockRepo = new WarehouseStockRepository(raceContext);
        var raceTxRepo = new InventoryTransactionRepository(raceContext);

        var recovered = await uow.ExecuteStockMovementAtomicallyAsync(
            warehouseId.Value,
            variantId.Value,
            async ct =>
            {
                var stock = await raceStockRepo.GetByWarehouseAndVariantAsync(warehouseId, variantId, ct);
                stock!.Receive(10m); // Would push QuantityOnHand from 20 -> 30 if not rolled back
                await raceTxRepo.AddAsync(InventoryTransaction.Create(
                    warehouseId,
                    variantId,
                    InventoryTransactionType.Receipt,
                    10m,
                    referenceType: "Refund",
                    referenceId: refundId,
                    notes: "Duplicate refund restock attempt"), ct);
                return stock.QuantityOnHand;
            },
            async ct =>
            {
                var committed = await raceStockRepo.GetByWarehouseAndVariantAsync(warehouseId, variantId, ct);
                return committed!.QuantityOnHand;
            });

        Assert.Equal(20m, recovered);

        await using var verifyContext = CreateContext();
        var finalStock = await new WarehouseStockRepository(verifyContext).GetByWarehouseAndVariantAsync(warehouseId, variantId);
        var txns = await new InventoryTransactionRepository(verifyContext).GetByReferenceAsync("Refund", refundId);
        Assert.Equal(20m, finalStock!.QuantityOnHand);
        Assert.Single(txns);
    }
}
