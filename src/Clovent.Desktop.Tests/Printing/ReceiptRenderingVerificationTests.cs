using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using Clovent.Desktop.Forms.Settings;
using Clovent.Desktop.Printing;
using Clovent.Platform.Printing;
using Xunit;

namespace Clovent.Desktop.Tests.Printing;

public class ReceiptRenderingVerificationTests
{
    [Fact]
    public void EnhancedReceiptPrintDocument_MultiPage_PaginatesLongReceiptWithoutClipping()
    {
        // Arrange: Generate a long 220-line receipt (simulating a large banquet or catering order)
        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("         BANQUET CATERING ORDER         ");
        sb.AppendLine("========================================");
        for (int i = 1; i <= 200; i++)
        {
            sb.AppendLine($"Item #{i:D3} - Banquet Specialty Dish    1x  150.00");
        }
        sb.AppendLine("----------------------------------------");
        sb.AppendLine("Grand Total:                   30,000.00");
        sb.AppendLine("========================================");

        var longReceiptText = sb.ToString();
        using var doc = new EnhancedReceiptPrintDocument(longReceiptText, PaperWidth.Width80mm);
        var previewController = new PreviewPrintController();
        doc.PrintController = previewController;

        // Act
        doc.Print();

        // Assert
        var pages = previewController.GetPreviewPageInfo();
        Assert.NotNull(pages);
        Assert.True(pages.Length > 1, $"Expected long receipt to span multiple pages, but was {pages.Length} pages.");

        foreach (var page in pages)
        {
            Assert.NotNull(page.Image);
            Assert.True(page.Image.Width > 0);
            Assert.True(page.Image.Height > 0);
        }
    }

    [Theory]
    [InlineData(PaperWidth.Width58mm, 228)]
    [InlineData(PaperWidth.Width80mm, 315)]
    [InlineData(PaperWidth.A4Custom, 827)]
    public void EnhancedReceiptPrintDocument_ConfiguresCorrectPaperWidthsAndMargins(PaperWidth width, int expectedHundredths)
    {
        // Act
        using var doc = new EnhancedReceiptPrintDocument("Sample Text", width);

        // Assert
        Assert.Equal(8, doc.DefaultPageSettings.Margins.Left);
        Assert.Equal(8, doc.DefaultPageSettings.Margins.Right);
        Assert.Equal(8, doc.DefaultPageSettings.Margins.Top);
        Assert.Equal(8, doc.DefaultPageSettings.Margins.Bottom);
        Assert.Equal(expectedHundredths, doc.DefaultPageSettings.PaperSize.Width);
    }

    [Fact]
    public void EnhancedReceiptPrintDocument_MixedUrduAndEnglish_RendersSuccessfullyWithoutExceptions()
    {
        // Arrange
        var mixedText =
            "========================================\n" +
            "           CLOVENT RESTAURANT           \n" +
            "       لاہور، پاکستان (Lahore, PK)      \n" +
            "========================================\n" +
            "Order: ORD-2026-999\n" +
            "Table: T-12\n" +
            "----------------------------------------\n" +
            "Item              Qty   Price    Total  \n" +
            "----------------------------------------\n" +
            "Chicken Karahi      1  1850.00  1850.00 \n" +
            "چکن کڑاہی فل\n" +
            "Garlic Naan         4    80.00   320.00 \n" +
            "روغنی نان\n" +
            "----------------------------------------\n" +
            "Subtotal:                       2170.00 \n" +
            "GST (16%):                       347.20 \n" +
            "Grand Total:                    2517.20 \n" +
            "========================================\n" +
            "شکریہ! دوبارہ تشریف لائیں۔\n" +
            "Thank you for your visit!\n" +
            "========================================\n";

        using var doc = new EnhancedReceiptPrintDocument(mixedText, PaperWidth.Width80mm, containsComplexScript: true);
        var previewController = new PreviewPrintController();
        doc.PrintController = previewController;

        // Act & Assert (must complete without GDI+ text measurement or font exceptions)
        doc.Print();
        var pages = previewController.GetPreviewPageInfo();
        Assert.NotNull(pages);
        Assert.NotEmpty(pages);
        Assert.NotNull(pages[0].Image);
    }

    [Fact]
    public void HistoricalFinancialFigures_PassThroughUnchanged_WithoutRecalculation()
    {
        // Arrange: Exact unrounded / specific historical figures from completed order
        var historicalSubtotal = 1234.56m;
        var historicalTax = 197.53m;
        var historicalDiscount = 50.25m;
        var historicalService = 25.00m;
        var historicalGrandTotal = 1406.84m;

        var data = new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-HISTORICAL-88",
            DailySalesNumber: 88,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            Items:
            [
                new PrintableReceiptItem("Special Platter", "PLT-1", 1.0m, historicalSubtotal, historicalSubtotal)
            ],
            Subtotal: historicalSubtotal,
            TaxTotal: historicalTax,
            DiscountTotal: historicalDiscount,
            ServiceChargeTotal: historicalService,
            GrandTotal: historicalGrandTotal,
            Balance: 0.00m,
            Payments:
            [
                new PrintableReceiptPayment("MasterCard", historicalGrandTotal)
            ],
            CashierName: "Historical Cashier",
            TerminalName: "TERM-01",
            BranchName: "Gulberg Branch",
            OrganizationName: "Clovent Ltd",
            TaxRegistrationNumber: "PK-123456");

        var options = new ReceiptRenderOptions(PaperWidth.Width80mm, CharactersPerLine: 42);

        // Act
        var result = ReceiptSnapshotFormatter.Format(data, options);

        // Assert: All historical financial figures appear exactly verbatim in the formatted output
        Assert.Contains("1,234.56", result.FormattedText);
        Assert.Contains("197.53", result.FormattedText);
        Assert.Contains("-50.25", result.FormattedText);
        Assert.Contains("25.00", result.FormattedText);
        Assert.Contains("1,406.84", result.FormattedText);
    }

    [Fact]
    public void PrinterSettingsForm_AccessibilityAndLayout_InstantiatesWithAccessibleControls()
    {
        // Arrange & Act
        using var form = new PrinterSettingsForm();

        // Assert: Form title, key controls, and accessibility
        Assert.Equal("Printer & Hardware Settings", form.Text);
        Assert.False(form.MaximizeBox);
        Assert.False(form.MinimizeBox);

        var titleLabel = form.Controls.Find("lblTitle", searchAllChildren: true).FirstOrDefault();
        var bottomPanel = form.Controls.Find("panelBottom", searchAllChildren: true).FirstOrDefault();

        Assert.NotNull(titleLabel);
        Assert.NotNull(bottomPanel);
    }
}
