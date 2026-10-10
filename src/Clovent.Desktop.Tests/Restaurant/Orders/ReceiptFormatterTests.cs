using System;
using System.Collections.Generic;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Application.Refunds.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Refunds;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public class ReceiptFormatterTests
{
    [Fact]
    public void FormatFromSnapshot_WithTaxableZeroRatedAndExemptItems_PresentsTaxTagsAndSummary()
    {
        var items = new List<ReceiptSnapshotItem>
        {
            new(Guid.NewGuid(), "SKU-BURGER", "Beef Burger", 1m, 1000m, 1000m, null,
                "Taxable", "PK-PRA-16", 16m, false, 1000m, 160m, 0m),
            new(Guid.NewGuid(), "SKU-BREAD", "Roti Bread", 2m, 50m, 100m, null,
                "Exempt", "PK-FBR-EXEMPT", 0m, false, 100m, 0m, 0m),
            new(Guid.NewGuid(), "SKU-EXPORT", "Rice Packet", 1m, 200m, 200m, null,
                "ZeroRated", "PK-FBR-ZERO", 0m, false, 200m, 0m, 0m)
        };

        var taxBreakdown = new List<OrderTaxSummarySnapshot>
        {
            new("PRA", "PK-PRA-16", 16m, "Taxable", false, 1000m, 160m)
        };

        var snapshot = new ReceiptSnapshot(
            Guid.NewGuid(),
            "ORD-2026-0001",
            1,
            "DineIn",
            DateTimeOffset.UtcNow,
            items,
            1300m,
            160m,
            0m,
            0m,
            1460m,
            0m,
            [new ReceiptSnapshotPayment(1460m, "Cash")],
            "Alice",
            "POS-01",
            null,
            taxBreakdown,
            "1.3.0-AwayFromZero-v1",
            1000m,
            160m,
            0m);

        var formatted = ReceiptFormatter.FormatFromSnapshot(snapshot);

        // Check invoice headers and items
        Assert.Contains("Clovent Business Operating System", formatted);
        Assert.Contains("ORD-2026-0001", formatted);
        Assert.Contains("Beef Burger", formatted);
        Assert.Contains("[T: 16% Exc]", formatted);
        Assert.Contains("Roti Bread", formatted);
        Assert.Contains("[E]", formatted);
        Assert.Contains("Rice Packet", formatted);
        Assert.Contains("[Z]", formatted);

        // Check statutory tax summary presentation
        Assert.Contains("Tax Summary:", formatted);
        Assert.Contains("PK-PRA-16 (16%)", formatted);
        Assert.Contains("1,000.00", formatted);
        Assert.Contains("160.00", formatted);
        Assert.Contains("Exclusive Tax:", formatted);
        Assert.Contains("1,460.00", formatted);
    }

    [Fact]
    public void FormatRefundCreditNote_FormatsCreditNoteDetailsAndOriginalReference()
    {
        var refundLines = new List<RefundLineDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SKU-BURGER", "Beef Burger",
                1m, 1000m, 1000m, 0m, 160m, 1160m, "Taxable", "PK-PRA-16", 16m, false, "Restock")
        };

        var refundDto = new RefundDto(
            Guid.NewGuid(),
            "REF-20261008-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Alice Cashier",
            "Cold food complaint",
            null,
            null,
            "REQ-IDEM-01",
            1000m,
            0m,
            160m,
            1160m,
            "CashPayout",
            null,
            null,
            null,
            refundLines);

        var creditNote = ReceiptFormatter.FormatRefundCreditNote(refundDto, "ORD-2026-0001");

        Assert.Contains("CREDIT NOTE / REFUND RECEIPT", creditNote);
        Assert.Contains("REF-20261008-001", creditNote);
        Assert.Contains("Original Invoice: ORD-2026-0001", creditNote);
        Assert.Contains("Reason: Cold food complaint", creditNote);
        Assert.Contains("Cashier: Alice Cashier", creditNote);
        Assert.Contains("Beef Burger", creditNote);
        Assert.Contains("Tax Reversed:", creditNote);
        Assert.Contains("160.00", creditNote);
        Assert.Contains("Total Refund Payable:", creditNote);
        Assert.Contains("1,160.00", creditNote);
        Assert.Contains("Settlement: CashPayout", creditNote);
    }
}
