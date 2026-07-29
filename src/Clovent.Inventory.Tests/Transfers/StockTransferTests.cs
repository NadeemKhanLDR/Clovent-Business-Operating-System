using Clovent.Catalog.Variants;
using Clovent.Inventory.Transfers;
using Clovent.Inventory.Transfers.Events;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Tests.Transfers;

public class StockTransferTests
{
    [Fact]
    public void Create_Valid_PendingByDefault_RaisesStockTransferCreated()
    {
        var sourceId = WarehouseId.New();
        var destinationId = WarehouseId.New();

        var transfer = StockTransfer.Create(sourceId, destinationId, ProductVariantId.New(), 10);

        Assert.Equal(sourceId, transfer.SourceWarehouseId);
        Assert.Equal(destinationId, transfer.DestinationWarehouseId);
        Assert.Equal(StockTransferStatus.Pending, transfer.Status);
        Assert.IsType<StockTransferCreated>(Assert.Single(transfer.DomainEvents));
    }

    [Fact]
    public void Create_SameSourceAndDestination_Throws()
    {
        var warehouseId = WarehouseId.New();

        Assert.Throws<Clovent.Inventory.InventoryDomainException>(() =>
            StockTransfer.Create(warehouseId, warehouseId, ProductVariantId.New(), 10));
    }

    [Fact]
    public void Create_ZeroOrNegativeQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockTransfer.Create(WarehouseId.New(), WarehouseId.New(), ProductVariantId.New(), 0));
    }

    [Fact]
    public void Complete_ThenCompleteAgain_Throws()
    {
        var transfer = StockTransfer.Create(WarehouseId.New(), WarehouseId.New(), ProductVariantId.New(), 10);
        transfer.ClearDomainEvents();

        transfer.Complete();

        Assert.Equal(StockTransferStatus.Completed, transfer.Status);
        Assert.NotNull(transfer.CompletedAtUtc);
        Assert.IsType<StockTransferCompleted>(Assert.Single(transfer.DomainEvents));
        Assert.Throws<Clovent.Inventory.InventoryDomainException>(() => transfer.Complete());
    }

    [Fact]
    public void Cancel_ThenComplete_Throws()
    {
        var transfer = StockTransfer.Create(WarehouseId.New(), WarehouseId.New(), ProductVariantId.New(), 10);

        transfer.Cancel();

        Assert.Equal(StockTransferStatus.Cancelled, transfer.Status);
        Assert.Throws<Clovent.Inventory.InventoryDomainException>(() => transfer.Complete());
    }
}
