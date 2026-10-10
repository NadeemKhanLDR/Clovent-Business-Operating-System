using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Restaurant.DomainServices;
using Xunit;

namespace Clovent.Restaurant.Tests.Taxation;

public class TaxCalculatorTests
{
    [Fact]
    public void Calculate_SingleLine_ExclusiveTax_ComputesCorrectly()
    {
        // Line: 2 units @ 500 PKR = 1000 PKR. Tax: 16% exclusive (PRA Services)
        var line = new TaxCalculationLineInput(
            Guid.NewGuid(),
            2m,
            500m,
            0m,
            "Taxable",
            "PRA",
            "PK-PRA-16",
            16.00m,
            false);

        var result = TaxCalculator.Calculate([line]);

        Assert.Equal(1000.00m, result.SubtotalGross);
        Assert.Equal(1000.00m, result.TotalTaxableBase);
        Assert.Equal(160.00m, result.TotalExclusiveTax);
        Assert.Equal(0.00m, result.TotalInclusiveTax);
        Assert.Equal(160.00m, result.TotalTax);
        Assert.Equal(1160.00m, result.TotalLinesPayable);

        var snapshot = Assert.Single(result.LineSnapshots);
        Assert.Equal(160.00m, snapshot.TaxAmount);
        Assert.Equal(1000.00m, snapshot.TaxableBase);
        Assert.Equal(1160.00m, snapshot.LinePayable);
    }

    [Fact]
    public void Calculate_SingleLine_InclusiveTax_ExtractsTaxExactWithoutDrift()
    {
        // Line: 1 unit @ 1160 PKR inclusive of 16% PRA tax.
        // PreTaxNet = RoundAwayFromZero(1160 / 1.16) = 1000.00
        // TaxAmount = 1160 - 1000 = 160.00
        var line = new TaxCalculationLineInput(
            Guid.NewGuid(),
            1m,
            1160m,
            0m,
            "Taxable",
            "PRA",
            "PK-PRA-16",
            16.00m,
            true);

        var result = TaxCalculator.Calculate([line]);

        Assert.Equal(1160.00m, result.SubtotalGross);
        Assert.Equal(1000.00m, result.TotalTaxableBase);
        Assert.Equal(0.00m, result.TotalExclusiveTax);
        Assert.Equal(160.00m, result.TotalInclusiveTax);
        Assert.Equal(160.00m, result.TotalTax);
        Assert.Equal(1160.00m, result.TotalLinesPayable);

        var snapshot = Assert.Single(result.LineSnapshots);
        Assert.Equal(1000.00m, snapshot.TaxableBase);
        Assert.Equal(160.00m, snapshot.TaxAmount);
        Assert.Equal(1160.00m, snapshot.LinePayable);
    }

    [Fact]
    public void Calculate_MixedTaxClassifications_DistinguishesTaxableZeroRatedExemptAndOutOfScope()
    {
        var taxableLine = new TaxCalculationLineInput(
            Guid.NewGuid(), 1m, 1000m, 0m, "Taxable", "PRA", "PK-PRA-16", 16.00m, false);

        var zeroRatedLine = new TaxCalculationLineInput(
            Guid.NewGuid(), 1m, 500m, 0m, "ZeroRated", "FBR", "PK-FBR-ZERO", 0.00m, false);

        var exemptLine = new TaxCalculationLineInput(
            Guid.NewGuid(), 1m, 300m, 0m, "Exempt", "FBR", "PK-FBR-EXEMPT", 0.00m, false);

        var outOfScopeLine = new TaxCalculationLineInput(
            Guid.NewGuid(), 1m, 200m, 0m, "OutOfScope", "NONE", "PK-OUT-OF-SCOPE", 0.00m, false);

        var result = TaxCalculator.Calculate([taxableLine, zeroRatedLine, exemptLine, outOfScopeLine]);

        Assert.Equal(2000.00m, result.SubtotalGross);
        Assert.Equal(160.00m, result.TotalTax); // Only taxable line incurs tax
        Assert.Equal(2160.00m, result.TotalLinesPayable);

        Assert.Equal(4, result.LineSnapshots.Count);
        Assert.Equal("Taxable", result.LineSnapshots[0].TaxClassification);
        Assert.Equal(160.00m, result.LineSnapshots[0].TaxAmount);

        Assert.Equal("ZeroRated", result.LineSnapshots[1].TaxClassification);
        Assert.Equal(0.00m, result.LineSnapshots[1].TaxAmount);

        Assert.Equal("Exempt", result.LineSnapshots[2].TaxClassification);
        Assert.Equal(0.00m, result.LineSnapshots[2].TaxAmount);

        Assert.Equal("OutOfScope", result.LineSnapshots[3].TaxClassification);
        Assert.Equal(0.00m, result.LineSnapshots[3].TaxAmount);

        // Tax summary grouping separates each tax code cleanly
        Assert.Equal(4, result.TaxSummary.Count);
    }

    [Fact]
    public void Calculate_OrderDiscountAllocation_HamiltonHareMethod_PennyExactNoResidualDrift()
    {
        // 3 items with prices 100, 100, 100 (Total 300).
        // Order discount: 100 PKR.
        // Exact fair split is 33.33333... per item.
        // Hamilton-Hare must allocate 33.34, 33.33, 33.33 summing to EXACTLY 100.00 PKR.
        var line1 = new TaxCalculationLineInput(Guid.NewGuid(), 1m, 100m, 0m, "Taxable", "PRA", "PK-PRA-16", 16.00m, false);
        var line2 = new TaxCalculationLineInput(Guid.NewGuid(), 1m, 100m, 0m, "Taxable", "PRA", "PK-PRA-16", 16.00m, false);
        var line3 = new TaxCalculationLineInput(Guid.NewGuid(), 1m, 100m, 0m, "Taxable", "PRA", "PK-PRA-16", 16.00m, false);

        var result = TaxCalculator.Calculate([line1, line2, line3], orderDiscountAmount: 100.00m);

        Assert.Equal(100.00m, result.TotalOrderDiscounts);
        var sumAllocated = result.LineSnapshots.Sum(s => s.AllocatedOrderDiscountAmount);
        Assert.Equal(100.00m, sumAllocated);

        // One line gets 33.34, two lines get 33.33
        var discounts = result.LineSnapshots.Select(s => s.AllocatedOrderDiscountAmount).OrderByDescending(d => d).ToList();
        Assert.Equal(33.34m, discounts[0]);
        Assert.Equal(33.33m, discounts[1]);
        Assert.Equal(33.33m, discounts[2]);
    }

    [Fact]
    public void Calculate_WithNetOfDiscountTaxMode_ComputesTaxOnDiscountedBase()
    {
        // Line: 1000 PKR, 100 PKR discount. Under net-of-discount mode (discountAppliesBeforeTax = true):
        // Taxable base = 900 PKR. 16% exclusive tax = 144 PKR.
        var line = new TaxCalculationLineInput(Guid.NewGuid(), 1m, 1000m, 0m, "Taxable", "PRA", "PK-PRA-16", 16.00m, false);

        var result = TaxCalculator.Calculate([line], orderDiscountAmount: 100.00m, discountAppliesBeforeTax: true);

        Assert.Equal(900.00m, result.TotalTaxableBase);
        Assert.Equal(144.00m, result.TotalExclusiveTax);
        Assert.Equal(1044.00m, result.TotalLinesPayable);
    }
}
