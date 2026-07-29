using Clovent.Catalog.Variants;
using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Application.Adjustments.Commands;
using Clovent.Inventory.Application.Adjustments.Queries;
using Clovent.Inventory.Application.Tests.TestSupport;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Application.Tests.Adjustments;

public class StockAdjustmentHandlerTests
{
    [Fact]
    public async Task CreateStockAdjustmentCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeStockAdjustmentRepository();
        var handler = new CreateStockAdjustmentCommandHandler(repository);

        var dto = await handler.Handle(
            new CreateStockAdjustmentCommand(Guid.NewGuid(), Guid.NewGuid(), StockAdjustmentType.Increase, 10, "Physical count correction"),
            CancellationToken.None);

        Assert.Equal("Pending", dto.Status);
        Assert.NotNull(await repository.GetByIdAsync(new StockAdjustmentId(dto.StockAdjustmentId)));
    }

    [Fact]
    public async Task ApplyStockAdjustmentCommandHandler_Increase_CreatesStockAndRecordsTransaction()
    {
        var adjustmentRepository = new FakeStockAdjustmentRepository();
        var stockRepository = new FakeWarehouseStockRepository();
        var transactionRepository = new FakeInventoryTransactionRepository();
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var adjustment = StockAdjustment.Create(warehouseId, variantId, StockAdjustmentType.Increase, 15, "Physical count correction");
        adjustmentRepository.Add(adjustment);
        var handler = new ApplyStockAdjustmentCommandHandler(adjustmentRepository, stockRepository, transactionRepository);

        var dto = await handler.Handle(new ApplyStockAdjustmentCommand(adjustment.Id.Value), CancellationToken.None);

        Assert.Equal("Applied", dto.Status);
        var stock = await stockRepository.GetByWarehouseAndVariantAsync(warehouseId, variantId);
        Assert.NotNull(stock);
        Assert.Equal(15m, stock!.QuantityOnHand);
        var transactions = await transactionRepository.GetByWarehouseIdAsync(warehouseId);
        Assert.Single(transactions);
        Assert.Equal(Clovent.Inventory.Transactions.InventoryTransactionType.Adjustment, transactions.Single().TransactionType);
    }

    [Fact]
    public async Task ApplyStockAdjustmentCommandHandler_Decrease_IssuesFromExistingStock()
    {
        var adjustmentRepository = new FakeStockAdjustmentRepository();
        var stockRepository = new FakeWarehouseStockRepository();
        var transactionRepository = new FakeInventoryTransactionRepository();
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var stock = Clovent.Inventory.WarehouseStocks.WarehouseStock.Create(warehouseId, variantId);
        stock.Receive(20);
        stockRepository.Add(stock);
        var adjustment = StockAdjustment.Create(warehouseId, variantId, StockAdjustmentType.Decrease, 5, "Damaged goods");
        adjustmentRepository.Add(adjustment);
        var handler = new ApplyStockAdjustmentCommandHandler(adjustmentRepository, stockRepository, transactionRepository);

        await handler.Handle(new ApplyStockAdjustmentCommand(adjustment.Id.Value), CancellationToken.None);

        Assert.Equal(15m, stock.QuantityOnHand);
    }

    [Fact]
    public async Task ApplyStockAdjustmentCommandHandler_AlreadyApplied_Throws()
    {
        var adjustmentRepository = new FakeStockAdjustmentRepository();
        var stockRepository = new FakeWarehouseStockRepository();
        var transactionRepository = new FakeInventoryTransactionRepository();
        var adjustment = StockAdjustment.Create(WarehouseId.New(), ProductVariantId.New(), StockAdjustmentType.Increase, 10, "Reason");
        adjustment.Apply();
        adjustmentRepository.Add(adjustment);
        var handler = new ApplyStockAdjustmentCommandHandler(adjustmentRepository, stockRepository, transactionRepository);

        await Assert.ThrowsAsync<Clovent.Inventory.InventoryDomainException>(() =>
            handler.Handle(new ApplyStockAdjustmentCommand(adjustment.Id.Value), CancellationToken.None));
    }

    [Fact]
    public async Task CancelStockAdjustmentCommandHandler_UnknownAdjustment_Throws()
    {
        var handler = new CancelStockAdjustmentCommandHandler(new FakeStockAdjustmentRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CancelStockAdjustmentCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListStockAdjustmentsQueryHandler_ReturnsEveryAdjustment()
    {
        var repository = new FakeStockAdjustmentRepository();
        repository.Add(StockAdjustment.Create(WarehouseId.New(), ProductVariantId.New(), StockAdjustmentType.Increase, 5, "Reason A"));
        repository.Add(StockAdjustment.Create(WarehouseId.New(), ProductVariantId.New(), StockAdjustmentType.Decrease, 3, "Reason B"));
        var handler = new ListStockAdjustmentsQueryHandler(repository);

        var result = await handler.Handle(new ListStockAdjustmentsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
