using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Application.Refunds.Dtos;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public sealed class ReportingReconciliationFormatterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FrozenReceipt_PresentsExclusiveOrIncludedTaxWithoutAddingItTwice(bool inclusive)
    {
        var snapshot = new ReceiptSnapshot(Guid.NewGuid(), "SYNTHETIC-AUDIT", null, "TakeAway",
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero),
            inclusive
                ? [new(Guid.NewGuid(), "A", "Inclusive item", 1m, 10.05m, 10.05m, null, "Taxable", "T10", 10m, true, 9.14m, .91m, 0m)]
                : [new(Guid.NewGuid(), "A", "Taxable item", 2m, 100m, 200m, null, "Taxable", "T10", 10m, false, 180m, 18m, 20m),
                   new(Guid.NewGuid(), "B", "Exempt item", 1m, 50m, 50m, null, "Exempt", "E", 0m, false, 45m, 0m, 5m)],
            inclusive ? 10.05m : 250m, inclusive ? .91m : 18m, inclusive ? 0m : 25m,
            0m, inclusive ? 10.05m : 243m, 0m,
            [new(inclusive ? 10.05m : 243m, "Cash")], "Synthetic", "AUDIT", null,
            TotalTaxableBase: inclusive ? 9.14m : 225m,
            TotalExclusiveTax: inclusive ? 0m : 18m, TotalInclusiveTax: inclusive ? .91m : 0m);
        var text = ReceiptFormatter.FormatFromSnapshot(snapshot);
        // Match a labelled line; an amount appearing elsewhere is insufficient.
        AssertLine(text, "Grand Total:", inclusive ? "10.05" : "243.00");
        AssertLine(text, inclusive ? "Included Tax (in prices):" : "Exclusive Tax:", inclusive ? "0.91" : "18.00");
        if (!inclusive) AssertLine(text, "Discounts:", "25.00");
        AssertLine(text, "Balance:", "0.00");
        AssertLine(text, "Item calculation base:", inclusive ? "9.14" : "225.00");
        Assert.DoesNotContain("Net Taxable Sales:", text);
    }

    [Theory]
    [InlineData("5.03", "0.46")]
    [InlineData("5.02", "0.45")]
    public void PartialInclusiveCreditNote_UsesStoredResidualAmounts(string netText, string taxText)
    {
        var net = decimal.Parse(netText, System.Globalization.CultureInfo.InvariantCulture);
        var tax = decimal.Parse(taxText, System.Globalization.CultureInfo.InvariantCulture);
        var refund = new RefundDto(Guid.NewGuid(), "SYNTHETIC-REFUND", Guid.NewGuid(), Guid.NewGuid(),
            new DateTimeOffset(2026, 1, 11, 12, 0, 0, TimeSpan.Zero), Guid.NewGuid(), "Synthetic",
            "Synthetic return", null, null, "synthetic-key", net, 0m, tax, net, "CashPayout", null, null, null,
            [new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "A", "Inclusive item", .5m, 10.05m,
                net, 0m, tax, net, "Taxable", "T10", 10m, true, "Restock")]);
        var text = ReceiptFormatter.FormatRefundCreditNote(refund, "SYNTHETIC-AUDIT");
        AssertLine(text, "Total Refund Payable:", netText);
        AssertLine(text, "Tax Reversed:", taxText);
    }

    private static void AssertLine(string text, string label, string amount)
    {
        var line = Assert.Single(text.Split('\n'), l => l.StartsWith(label, StringComparison.Ordinal));
        Assert.Contains(amount, line);
    }
}
