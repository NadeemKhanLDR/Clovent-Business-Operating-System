using System.Text.Json;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Printing;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Platform.Printing;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public class OrderCompletedDialogTests
{
    [Fact]
    public void Constructor_Parameterless_InitializesSuccessfullyForDesigner()
    {
        using var dialog = new OrderCompletedDialog();
        Assert.NotNull(dialog);
        Assert.Equal("Order Completed", dialog.Text);
        Assert.False(dialog.MaximizeBox);
        Assert.False(dialog.MinimizeBox);
    }

    [Fact]
    public void Controls_AreProperlyInstantiated()
    {
        using var dialog = new OrderCompletedDialog();

        Assert.NotEmpty(dialog.Controls);
        var header = dialog.Controls.Find("lblHeader", searchAllChildren: true);
        var orderNum = dialog.Controls.Find("lblOrderNumber", searchAllChildren: true);
        var invoiceNum = dialog.Controls.Find("lblInvoiceNumber", searchAllChildren: true);
        var summary = dialog.Controls.Find("grpSummary", searchAllChildren: true);
        var grandTotal = dialog.Controls.Find("lblGrandTotal", searchAllChildren: true);
        var tendered = dialog.Controls.Find("lblTendered", searchAllChildren: true);
        var changeDue = dialog.Controls.Find("lblChangeDue", searchAllChildren: true);
        var printStatus = dialog.Controls.Find("grpPrintStatus", searchAllChildren: true);
        var btnPrint = dialog.Controls.Find("btnPrintReceipt", searchAllChildren: true);
        var btnReprint = dialog.Controls.Find("btnReprintReceipt", searchAllChildren: true);
        var btnDone = dialog.Controls.Find("btnDone", searchAllChildren: true);

        Assert.NotEmpty(header);
        Assert.NotEmpty(orderNum);
        Assert.NotEmpty(invoiceNum);
        Assert.NotEmpty(summary);
        Assert.NotEmpty(grandTotal);
        Assert.NotEmpty(tendered);
        Assert.NotEmpty(changeDue);
        Assert.NotEmpty(printStatus);
        Assert.NotEmpty(btnPrint);
        Assert.NotEmpty(btnReprint);
        Assert.NotEmpty(btnDone);
    }

    [Fact]
    public void PopulateDisplay_RendersOrderFinancialsAndSpoolerStatus()
    {
        var snapshot = new ReceiptSnapshot(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-TEST-888",
            DailySalesNumber: 42,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items:
            [
                new(Guid.NewGuid(), "BURGER-01", "Special Burger", 2, 20.00m, 40.00m, null)
            ],
            Subtotal: 40.00m,
            TaxTotal: 3.20m,
            DiscountTotal: 0m,
            ServiceChargeTotal: 0m,
            GrandTotal: 43.20m,
            Balance: 0m,
            Payments:
            [
                new(50.00m, "Cash")
            ],
            CashierName: "Alice",
            TerminalName: "Term-01",
            CustomerNotes: null);

        var orderDto = new OrderDto(
            OrderId: snapshot.OrderId,
            OrderNumber: snapshot.OrderNumber,
            DailySalesNumber: 42,
            OrderType: "DineIn",
            Status: "Completed",
            TableId: null,
            WarehouseId: Guid.NewGuid(),
            Notes: null,
            CustomerNotes: null,
            OrderLineIds: [],
            DiscountIds: [],
            ServiceChargeIds: [],
            PaymentIds: [],
            CreatedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            CustomerId: null,
            ReceiptSnapshotJson: JsonSerializer.Serialize(snapshot));

        var tracker = new InMemoryPrinterJobTracker();

        using var dialog = new OrderCompletedDialog(
            order: orderDto,
            grandTotal: 43.20m,
            totalTendered: 50.00m,
            changeDue: 6.80m,
            jobTracker: tracker);

        // Force handle creation to trigger Load event
        var handle = dialog.Handle;

        var lblOrder = dialog.Controls.Find("lblOrderNumber", searchAllChildren: true)[0];
        var lblInvoice = dialog.Controls.Find("lblInvoiceNumber", searchAllChildren: true)[0];
        var lblGrand = dialog.Controls.Find("lblGrandTotal", searchAllChildren: true)[0];
        var lblTender = dialog.Controls.Find("lblTendered", searchAllChildren: true)[0];
        var lblChange = dialog.Controls.Find("lblChangeDue", searchAllChildren: true)[0];

        Assert.Contains("ORD-TEST-888", lblOrder.Text);
        Assert.Contains("42", lblInvoice.Text);
        Assert.Contains("43.20", lblGrand.Text);
        Assert.Contains("50.00", lblTender.Text);
        Assert.Contains("6.80", lblChange.Text);
    }
}
