using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.Tests.TestSupport;
using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Application.Tests.WarehouseStocks;

public class WarehouseStockHandlerTests
{
    [Fact]
    public async Task CreateWarehouseStockCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeWarehouseStockRepository();
        var handler = new CreateWarehouseStockCommandHandler(repository);

        var dto = await handler.Handle(new CreateWarehouseStockCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(0m, dto.QuantityOnHand);
        Assert.NotNull(await repository.GetByIdAsync(new WarehouseStockId(dto.WarehouseStockId)));
    }

    [Fact]
    public async Task ReceiveStockCommandHandler_RecordsInventoryTransaction()
    {
        var stockRepository = new FakeWarehouseStockRepository();
        var transactionRepository = new FakeInventoryTransactionRepository();
        var stock = WarehouseStock.Create(WarehouseId.New(), ProductVariantId.New());
        stockRepository.Add(stock);
        var handler = new ReceiveStockCommandHandler(stockRepository, transactionRepository);

        var dto = await handler.Handle(new ReceiveStockCommand(stock.Id.Value, 10), CancellationToken.None);

        Assert.Equal(10m, dto.QuantityOnHand);
        var recorded = await transactionRepository.GetByWarehouseIdAsync(stock.WarehouseId);
        Assert.Single(recorded);
        Assert.Equal(Clovent.Inventory.Transactions.InventoryTransactionType.Receipt, recorded.Single().TransactionType);
    }

    [Fact]
    public async Task IssueStockCommandHandler_RecordsInventoryTransaction()
    {
        var stockRepository = new FakeWarehouseStockRepository();
        var transactionRepository = new FakeInventoryTransactionRepository();
        var stock = WarehouseStock.Create(WarehouseId.New(), ProductVariantId.New());
        stock.Receive(20);
        stockRepository.Add(stock);
        var handler = new IssueStockCommandHandler(stockRepository, transactionRepository);

        var dto = await handler.Handle(new IssueStockCommand(stock.Id.Value, 5), CancellationToken.None);

        Assert.Equal(15m, dto.QuantityOnHand);
        var recorded = await transactionRepository.GetByWarehouseIdAsync(stock.WarehouseId);
        Assert.Single(recorded);
        Assert.Equal(Clovent.Inventory.Transactions.InventoryTransactionType.Issue, recorded.Single().TransactionType);
    }

    [Fact]
    public async Task ReserveAndReleaseStockCommandHandlers_RoundTrip()
    {
        var repository = new FakeWarehouseStockRepository();
        var stock = WarehouseStock.Create(WarehouseId.New(), ProductVariantId.New());
        stock.Receive(10);
        repository.Add(stock);

        var reserved = await new ReserveStockCommandHandler(repository)
            .Handle(new ReserveStockCommand(stock.Id.Value, 4), CancellationToken.None);
        Assert.Equal(4m, reserved.QuantityReserved);

        var released = await new ReleaseStockReservationCommandHandler(repository)
            .Handle(new ReleaseStockReservationCommand(stock.Id.Value, 4), CancellationToken.None);
        Assert.Equal(0m, released.QuantityReserved);
    }

    [Fact]
    public async Task SetWarehouseStockLevelsCommandHandler_UnknownStock_Throws()
    {
        var handler = new SetWarehouseStockLevelsCommandHandler(new FakeWarehouseStockRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new SetWarehouseStockLevelsCommand(Guid.NewGuid(), 5, 10), CancellationToken.None));
    }

    [Fact]
    public async Task ListWarehouseStocksByWarehouseQueryHandler_FiltersToOwningWarehouse()
    {
        var repository = new FakeWarehouseStockRepository();
        var warehouseId = WarehouseId.New();
        repository.Add(WarehouseStock.Create(warehouseId, ProductVariantId.New()));
        repository.Add(WarehouseStock.Create(WarehouseId.New(), ProductVariantId.New()));
        var handler = new ListWarehouseStocksByWarehouseQueryHandler(repository);

        var result = await handler.Handle(new ListWarehouseStocksByWarehouseQuery(warehouseId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
