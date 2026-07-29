using Clovent.Catalog.Variants;
using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Adjustments.Events;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Tests.Adjustments;

public class StockAdjustmentTests
{
    private static StockAdjustment CreateAdjustment() =>
        StockAdjustment.Create(WarehouseId.New(), ProductVariantId.New(), StockAdjustmentType.Increase, 10, "Physical count correction");

    [Fact]
    public void Create_Valid_PendingByDefault_RaisesStockAdjustmentCreated()
    {
        var adjustment = CreateAdjustment();

        Assert.Equal(StockAdjustmentStatus.Pending, adjustment.Status);
        Assert.Null(adjustment.AppliedAtUtc);
        Assert.IsType<StockAdjustmentCreated>(Assert.Single(adjustment.DomainEvents));
    }

    [Fact]
    public void Create_ZeroOrNegativeQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockAdjustment.Create(WarehouseId.New(), ProductVariantId.New(), StockAdjustmentType.Increase, 0, "Reason"));
    }

    [Fact]
    public void Create_EmptyReason_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            StockAdjustment.Create(WarehouseId.New(), ProductVariantId.New(), StockAdjustmentType.Increase, 10, ""));
    }

    [Fact]
    public void Apply_ThenApplyAgain_Throws()
    {
        var adjustment = CreateAdjustment();
        adjustment.ClearDomainEvents();

        adjustment.Apply();

        Assert.Equal(StockAdjustmentStatus.Applied, adjustment.Status);
        Assert.NotNull(adjustment.AppliedAtUtc);
        Assert.IsType<StockAdjustmentApplied>(Assert.Single(adjustment.DomainEvents));
        Assert.Throws<Clovent.Inventory.InventoryDomainException>(() => adjustment.Apply());
    }

    [Fact]
    public void Cancel_ThenApply_Throws()
    {
        var adjustment = CreateAdjustment();

        adjustment.Cancel();

        Assert.Equal(StockAdjustmentStatus.Cancelled, adjustment.Status);
        Assert.Throws<Clovent.Inventory.InventoryDomainException>(() => adjustment.Apply());
    }
}
