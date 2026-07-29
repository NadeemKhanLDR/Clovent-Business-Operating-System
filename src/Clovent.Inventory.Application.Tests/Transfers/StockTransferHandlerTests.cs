using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.Tests.TestSupport;
using Clovent.Inventory.Application.Transfers.Commands;
using Clovent.Inventory.Application.Transfers.Queries;
using Clovent.Inventory.Transfers;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Application.Tests.Transfers;

public class StockTransferHandlerTests
{
    [Fact]
    public async Task CreateStockTransferCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeStockTransferRepository();
        var handler = new CreateStockTransferCommandHandler(repository);

        var dto = await handler.Handle(
            new CreateStockTransferCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10),
            CancellationToken.None);

        Assert.Equal("Pending", dto.Status);
        Assert.NotNull(await repository.GetByIdAsync(new StockTransferId(dto.StockTransferId)));
    }

    [Fact]
    public async Task CompleteStockTransferCommandHandler_MovesStockAndRecordsTwoTransactions()
    {
        var transferRepository = new FakeStockTransferRepository();
        var stockRepository = new FakeWarehouseStockRepository();
        var transactionRepository = new FakeInventoryTransactionRepository();
        var sourceWarehouseId = WarehouseId.New();
        var destinationWarehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();

        var sourceStock = WarehouseStock.Create(sourceWarehouseId, variantId);
        sourceStock.Receive(20);
        stockRepository.Add(sourceStock);

        var transfer = StockTransfer.Create(sourceWarehouseId, destinationWarehouseId, variantId, 8);
        transferRepository.Add(transfer);

        var handler = new CompleteStockTransferCommandHandler(transferRepository, stockRepository, transactionRepository);

        var dto = await handler.Handle(new CompleteStockTransferCommand(transfer.Id.Value), CancellationToken.None);

        Assert.Equal("Completed", dto.Status);
        Assert.Equal(12m, sourceStock.QuantityOnHand);

        var destinationStock = await stockRepository.GetByWarehouseAndVariantAsync(destinationWarehouseId, variantId);
        Assert.NotNull(destinationStock);
        Assert.Equal(8m, destinationStock!.QuantityOnHand);

        var sourceTransactions = await transactionRepository.GetByWarehouseIdAsync(sourceWarehouseId);
        var destinationTransactions = await transactionRepository.GetByWarehouseIdAsync(destinationWarehouseId);
        Assert.Single(sourceTransactions);
        Assert.Single(destinationTransactions);
    }

    [Fact]
    public async Task CompleteStockTransferCommandHandler_NoSourceStock_Throws()
    {
        var transferRepository = new FakeStockTransferRepository();
        var stockRepository = new FakeWarehouseStockRepository();
        var transactionRepository = new FakeInventoryTransactionRepository();
        var transfer = StockTransfer.Create(WarehouseId.New(), WarehouseId.New(), ProductVariantId.New(), 5);
        transferRepository.Add(transfer);
        var handler = new CompleteStockTransferCommandHandler(transferRepository, stockRepository, transactionRepository);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CompleteStockTransferCommand(transfer.Id.Value), CancellationToken.None));
    }

    [Fact]
    public async Task CancelStockTransferCommandHandler_UnknownTransfer_Throws()
    {
        var handler = new CancelStockTransferCommandHandler(new FakeStockTransferRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CancelStockTransferCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListStockTransfersQueryHandler_ReturnsEveryTransfer()
    {
        var repository = new FakeStockTransferRepository();
        repository.Add(StockTransfer.Create(WarehouseId.New(), WarehouseId.New(), ProductVariantId.New(), 5));
        repository.Add(StockTransfer.Create(WarehouseId.New(), WarehouseId.New(), ProductVariantId.New(), 3));
        var handler = new ListStockTransfersQueryHandler(repository);

        var result = await handler.Handle(new ListStockTransfersQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
