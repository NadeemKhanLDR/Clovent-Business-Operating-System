using Clovent.Restaurant.Application.Printing;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Printing;

public class ReceiptSnapshotMappingExtensionsTests
{
    [Fact]
    public void ToPrintable_PreservesAllFinancialSnapshotValuesVerbatim()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var completedAt = DateTimeOffset.UtcNow;
        var snapshot = new ReceiptSnapshot(
            OrderId: orderId,
            OrderNumber: "ORD-999",
            DailySalesNumber: 15,
            OrderType: "DineIn",
            CompletedAtUtc: completedAt,
            Items:
            [
                new ReceiptSnapshotItem(Guid.NewGuid(), "SKU-01", "Chicken Karahi Half", 1m, 1200m, 1200m, "Spicy"),
                new ReceiptSnapshotItem(Guid.NewGuid(), "SKU-02", "Roti", 4m, 25m, 100m, null)
            ],
            Subtotal: 1300m,
            TaxTotal: 208m,
            DiscountTotal: 130m,
            ServiceChargeTotal: 65m,
            GrandTotal: 1443m,
            Balance: 0m,
            Payments:
            [
                new ReceiptSnapshotPayment(1443m, "Cash")
            ],
            CashierName: "Usman Ali",
            TerminalName: "Counter-1",
            CustomerNotes: "VIP Guest");

        // Act
        var printable = snapshot.ToPrintable(
            branchName: "Gulberg III",
            organizationName: "Clovent Pakistan",
            taxRegistrationNumber: "STRN-1122334-5");

        // Assert
        Assert.NotNull(printable);
        Assert.Equal(orderId, printable.OrderId);
        Assert.Equal("ORD-999", printable.OrderNumber);
        Assert.Equal(15, printable.DailySalesNumber);
        Assert.Equal("DineIn", printable.OrderType);
        Assert.Equal(completedAt, printable.CompletedAtUtc);
        Assert.Equal(2, printable.Items.Count);
        Assert.Equal("Chicken Karahi Half", printable.Items[0].Name);
        Assert.Equal(1200m, printable.Items[0].LineTotal);
        Assert.Equal("Spicy", printable.Items[0].Notes);
        Assert.Equal(1300m, printable.Subtotal);
        Assert.Equal(208m, printable.TaxTotal);
        Assert.Equal(130m, printable.DiscountTotal);
        Assert.Equal(65m, printable.ServiceChargeTotal);
        Assert.Equal(1443m, printable.GrandTotal);
        Assert.Equal(0m, printable.Balance);
        Assert.Single(printable.Payments);
        Assert.Equal(1443m, printable.Payments[0].Amount);
        Assert.Equal("Cash", printable.Payments[0].PaymentMethodName);
        Assert.Equal("Usman Ali", printable.CashierName);
        Assert.Equal("Counter-1", printable.TerminalName);
        Assert.Equal("Gulberg III", printable.BranchName);
        Assert.Equal("Clovent Pakistan", printable.OrganizationName);
        Assert.Equal("STRN-1122334-5", printable.TaxRegistrationNumber);
        Assert.Equal("VIP Guest", printable.CustomerNotes);
    }
}
