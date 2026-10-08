using System;
using Clovent.Catalog.Variants;
using Clovent.Identity.Branches;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Refunds;
using Xunit;

namespace Clovent.Restaurant.Tests.Refunds;

public class RefundAggregateTests
{
    [Fact]
    public void Create_ValidParameters_InstantiatesRefundAggregateCorrectly()
    {
        var orderId = OrderId.New();
        var branchId = BranchId.New();
        var warehouseId = WarehouseId.New();
        var cashierId = Guid.NewGuid();
        var refundNumber = RefundNumber.Generate(DateTimeOffset.UtcNow);

        var refund = Refund.Create(
            refundNumber,
            orderId,
            branchId,
            warehouseId,
            cashierId,
            "John Cashier",
            "Customer returned cold food",
            "REQ-IDEM-001",
            RefundSettlementMethod.CashPayout);

        Assert.Equal(refundNumber, refund.RefundNumber);
        Assert.Equal(orderId, refund.OrderId);
        Assert.Equal(branchId, refund.BranchId);
        Assert.Equal(warehouseId, refund.WarehouseId);
        Assert.Equal("John Cashier", refund.CashierName);
        Assert.Equal("Customer returned cold food", refund.Reason);
        Assert.Equal("REQ-IDEM-001", refund.IdempotencyKey);
        Assert.Equal(RefundSettlementMethod.CashPayout, refund.SettlementMethod);
        Assert.Empty(refund.Lines);
    }

    [Theory]
    [InlineData("", "Valid Reason", "IDEM-1")]
    [InlineData("Valid Cashier", "", "IDEM-1")]
    [InlineData("Valid Cashier", "Valid Reason", "")]
    public void Create_MissingRequiredFields_ThrowsArgumentException(
        string cashier, string reason, string idempotencyKey)
    {
        var orderId = OrderId.New();
        var branchId = BranchId.New();
        var warehouseId = WarehouseId.New();
        var refundNumber = RefundNumber.Generate(DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            Refund.Create(
                refundNumber,
                orderId,
                branchId,
                warehouseId,
                Guid.NewGuid(),
                cashier,
                reason,
                idempotencyKey,
                RefundSettlementMethod.CashPayout));
    }

    [Fact]
    public void RefundNumber_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RefundNumber(""));
    }

    [Fact]
    public void AddLine_And_FinalizeFinancials_SetsTotalsAccurately()
    {
        var refund = Refund.Create(
            RefundNumber.Generate(DateTimeOffset.UtcNow),
            OrderId.New(),
            BranchId.New(),
            WarehouseId.New(),
            Guid.NewGuid(),
            "Cashier",
            "Defective Item",
            "IDEM-2",
            RefundSettlementMethod.CashPayout);

        var line = RefundLine.Create(
            refund.Id,
            OrderLineId.New(),
            ProductVariantId.New(),
            "SKU-100",
            "Spicy Burger",
            1m,
            500m,
            50m,
            72m,
            522m,
            "Taxable",
            "PK-PRA-16",
            16m,
            false,
            InventoryDisposition.Restock);

        refund.AddLine(line);
        refund.FinalizeFinancials(500m, 50m, 72m, 522m, "{\"Receipt\":\"Mock\"}");

        Assert.Single(refund.Lines);
        Assert.Equal(500m, refund.SubtotalRefunded);
        Assert.Equal(50m, refund.DiscountReversedTotal);
        Assert.Equal(72m, refund.TaxReversedTotal);
        Assert.Equal(522m, refund.GrandTotalRefunded);
        Assert.NotNull(refund.ReceiptSnapshotJson);
    }
}
