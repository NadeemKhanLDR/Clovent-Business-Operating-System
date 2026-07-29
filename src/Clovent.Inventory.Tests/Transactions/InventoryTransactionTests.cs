using Clovent.Catalog.Variants;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.Transactions.Events;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Tests.Transactions;

public class InventoryTransactionTests
{
    [Fact]
    public void Create_Valid_RaisesInventoryTransactionRecorded()
    {
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();

        var transaction = InventoryTransaction.Create(warehouseId, variantId, InventoryTransactionType.Receipt, 10);

        Assert.Equal(warehouseId, transaction.WarehouseId);
        Assert.Equal(variantId, transaction.ProductVariantId);
        Assert.Equal(InventoryTransactionType.Receipt, transaction.TransactionType);
        Assert.Equal(10m, transaction.Quantity);
        Assert.IsType<InventoryTransactionRecorded>(Assert.Single(transaction.DomainEvents));
    }

    [Fact]
    public void Create_WithReference_SetsReferenceFields()
    {
        var referenceId = Guid.NewGuid();

        var transaction = InventoryTransaction.Create(
            WarehouseId.New(), ProductVariantId.New(), InventoryTransactionType.Adjustment, 5,
            referenceType: "StockAdjustment", referenceId: referenceId, notes: "Physical count correction");

        Assert.Equal("StockAdjustment", transaction.ReferenceType);
        Assert.Equal(referenceId, transaction.ReferenceId);
        Assert.Equal("Physical count correction", transaction.Notes);
    }

    [Fact]
    public void Create_ZeroOrNegativeQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InventoryTransaction.Create(WarehouseId.New(), ProductVariantId.New(), InventoryTransactionType.Issue, 0));
    }
}
