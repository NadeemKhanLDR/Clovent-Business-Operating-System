using Clovent.Catalog.Variants;
using Clovent.Inventory.Infrastructure.Repositories;
using Clovent.Inventory.Infrastructure.Tests.TestSupport;
using Clovent.Inventory.Transfers;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Infrastructure.Tests.Repositories;

public class StockTransferRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var sourceWarehouseId = WarehouseId.New();
        var destinationWarehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var transfer = StockTransfer.Create(sourceWarehouseId, destinationWarehouseId, variantId, 12);

        await using (var writeContext = CreateContext())
        {
            var repository = new StockTransferRepository(writeContext);
            await repository.AddAsync(transfer);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new StockTransferRepository(readContext).GetByIdAsync(transfer.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(sourceWarehouseId, reloaded!.SourceWarehouseId);
        Assert.Equal(destinationWarehouseId, reloaded.DestinationWarehouseId);
        Assert.Equal(variantId, reloaded.ProductVariantId);
        Assert.Equal(12, reloaded.Quantity);
        Assert.Equal(StockTransferStatus.Pending, reloaded.Status);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryTransfer()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new StockTransferRepository(writeContext);
            await repository.AddAsync(StockTransfer.Create(WarehouseId.New(), WarehouseId.New(), ProductVariantId.New(), 5));
            await repository.AddAsync(StockTransfer.Create(WarehouseId.New(), WarehouseId.New(), ProductVariantId.New(), 8));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new StockTransferRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new StockTransferRepository(context).GetByIdAsync(StockTransferId.New());

        Assert.Null(result);
    }
}
