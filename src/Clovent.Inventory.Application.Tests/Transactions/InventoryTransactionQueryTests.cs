using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.Tests.TestSupport;
using Clovent.Inventory.Application.Transactions.Queries;
using Clovent.Inventory.Transactions;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Application.Tests.Transactions;

public class InventoryTransactionQueryTests
{
    [Fact]
    public async Task GetInventoryTransactionByIdQueryHandler_UnknownTransaction_Throws()
    {
        var handler = new GetInventoryTransactionByIdQueryHandler(new FakeInventoryTransactionRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetInventoryTransactionByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListInventoryTransactionsByWarehouseQueryHandler_FiltersToOwningWarehouse()
    {
        var repository = new FakeInventoryTransactionRepository();
        var warehouseId = WarehouseId.New();
        repository.Add(InventoryTransaction.Create(warehouseId, ProductVariantId.New(), InventoryTransactionType.Receipt, 10));
        repository.Add(InventoryTransaction.Create(WarehouseId.New(), ProductVariantId.New(), InventoryTransactionType.Receipt, 5));
        var handler = new ListInventoryTransactionsByWarehouseQueryHandler(repository);

        var result = await handler.Handle(new ListInventoryTransactionsByWarehouseQuery(warehouseId.Value), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task ListRecentInventoryTransactionsQueryHandler_RespectsCount()
    {
        var repository = new FakeInventoryTransactionRepository();
        for (var i = 0; i < 5; i++)
        {
            repository.Add(InventoryTransaction.Create(WarehouseId.New(), ProductVariantId.New(), InventoryTransactionType.Receipt, 1));
        }
        var handler = new ListRecentInventoryTransactionsQueryHandler(repository);

        var result = await handler.Handle(new ListRecentInventoryTransactionsQuery(3), CancellationToken.None);

        Assert.Equal(3, result.Count);
    }
}
