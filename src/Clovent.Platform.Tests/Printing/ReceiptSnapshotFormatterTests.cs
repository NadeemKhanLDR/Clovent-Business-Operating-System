using Clovent.Platform.Printing;
using Xunit;

namespace Clovent.Platform.Tests.Printing;

public class ReceiptSnapshotFormatterTests
{
    private PrintableReceiptData CreateSampleData(string? notes = null, string? itemName = null)
    {
        return new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-2026-0001",
            DailySalesNumber: 42,
            OrderType: "DineIn",
            CompletedAtUtc: new DateTimeOffset(2026, 10, 8, 14, 30, 0, TimeSpan.Zero),
            Items:
            [
                new PrintableReceiptItem(itemName ?? "Chicken Biryani Special", "SKU-001", 2.0m, 650.00m, 1300.00m, notes),
                new PrintableReceiptItem("Mineral Water 500ml", "SKU-002", 2.0m, 80.00m, 160.00m)
            ],
            Subtotal: 1460.00m,
            TaxTotal: 233.60m,
            DiscountTotal: 100.00m,
            ServiceChargeTotal: 50.00m,
            GrandTotal: 1643.60m,
            Balance: 0.00m,
            Payments:
            [
                new PrintableReceiptPayment("Cash", 1643.60m)
            ],
            CashierName: "Ahmad Raza",
            TerminalName: "POS-01",
            BranchName: "Gulberg Main",
            OrganizationName: "Clovent Hospitality Ltd",
            TaxRegistrationNumber: "STRN-1234567-8");
    }

    [Fact]
    public void Format_80mmWidth_ProducesTabularLayoutAndPreservesExactTotals()
    {
        // Arrange
        var data = CreateSampleData();
        var options = new ReceiptRenderOptions(PaperWidth.Width80mm, CharactersPerLine: 42);

        // Act
        var result = ReceiptSnapshotFormatter.Format(data, options);

        // Assert
        Assert.NotNull(result.FormattedText);
        Assert.Contains("ORD-2026-0001", result.FormattedText);
        Assert.Contains("Chicken Biryani Special", result.FormattedText);
        Assert.Contains("Subtotal:", result.FormattedText);
        Assert.Contains("1,460.00", result.FormattedText);
        Assert.Contains("Tax:", result.FormattedText);
        Assert.Contains("233.60", result.FormattedText);
        Assert.Contains("Discount:", result.FormattedText);
        Assert.Contains("-100.00", result.FormattedText);
        Assert.Contains("Grand Total:", result.FormattedText);
        Assert.Contains("1,643.60", result.FormattedText);
        Assert.Contains("Cashier:", result.FormattedText);
        Assert.Contains("Ahmad Raza", result.FormattedText);
        Assert.False(result.ContainsComplexScript);
    }

    [Fact]
    public void Format_58mmNarrowWidth_WrapsLongLinesWithoutError()
    {
        // Arrange
        var data = CreateSampleData(itemName: "Very Long Specialty Dish Name Exceeding Narrow Roll Width");
        var options = new ReceiptRenderOptions(PaperWidth.Width58mm, CharactersPerLine: 32);

        // Act
        var result = ReceiptSnapshotFormatter.Format(data, options);

        // Assert
        Assert.NotNull(result.FormattedText);
        Assert.Contains("ORD-2026-0001", result.FormattedText);
        Assert.Contains("Grand Total:", result.FormattedText);
        Assert.Contains("1,643.60", result.FormattedText);
    }

    [Fact]
    public void Format_ReprintRequest_IncludesReprintAuditBanner()
    {
        // Arrange
        var data = CreateSampleData();
        var options = new ReceiptRenderOptions(
            PaperWidth.Width80mm,
            CharactersPerLine: 42,
            IsReprint: true,
            ReprintCount: 2,
            ReprintReason: "Customer requested receipt copy");

        // Act
        var result = ReceiptSnapshotFormatter.Format(data, options);

        // Assert
        Assert.Contains("*** REPRINT (COPY #2) ***", result.FormattedText);
        Assert.Contains("Reason: Customer requested receipt copy", result.FormattedText);
    }

    [Fact]
    public void Format_UrduScriptDetected_FlagsComplexScriptForRasterFallback()
    {
        // Arrange
        var data = CreateSampleData(itemName: "چکن بریانی اسپیشل");
        var options = new ReceiptRenderOptions(PaperWidth.Width80mm, CharactersPerLine: 42);

        // Act
        var result = ReceiptSnapshotFormatter.Format(data, options);

        // Assert
        Assert.True(result.ContainsComplexScript);
        Assert.Contains("چکن بریانی اسپیشل", result.FormattedText);
    }

    [Fact]
    public void FormatTestPrintSlip_IncludesDiagnosticDetailsAndCharacterRuler()
    {
        // Arrange
        var profile = new PrinterProfile
        {
            ProfileName = "Front Counter 80mm",
            SystemPrinterName = "POS-80",
            Role = PrinterRole.Receipt,
            PaperWidth = PaperWidth.Width80mm,
            CharactersPerLine = 42,
            SupportsCutter = true,
            SupportsCashDrawer = true
        };

        // Act
        var testSlip = ReceiptSnapshotFormatter.FormatTestPrintSlip(profile, "Lead Cashier");

        // Assert
        Assert.Contains("*** PRINTER DIAGNOSTIC TEST SLIP ***", testSlip);
        Assert.Contains("Front Counter 80mm", testSlip);
        Assert.Contains("CHARACTER RULER", testSlip);
        Assert.Contains("URDU SCRIPT TEST:", testSlip);
        Assert.Contains("HARDWARE TEST SUCCESSFUL", testSlip);
    }
}
