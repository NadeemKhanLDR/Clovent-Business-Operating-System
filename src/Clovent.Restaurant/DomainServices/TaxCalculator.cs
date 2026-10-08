using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.DomainServices;

/// <summary>
/// Input line specification for financial calculation and tax determination.
/// </summary>
public sealed record TaxCalculationLineInput(
    Guid OrderLineId,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineDiscountAmount = 0m,
    string TaxClassification = "Taxable",
    string Authority = "PRA",
    string TaxCode = "PK-PRA-16",
    decimal TaxRatePercentage = 16.00m,
    bool TaxIsInclusive = false);

/// <summary>
/// Comprehensive result of pure financial calculation and Pakistan tax determination.
/// </summary>
public sealed record OrderTaxCalculationResult(
    IReadOnlyList<LineTaxSnapshot> LineSnapshots,
    IReadOnlyList<OrderTaxSummarySnapshot> TaxSummary,
    decimal SubtotalGross,
    decimal TotalLineDiscounts,
    decimal TotalOrderDiscounts,
    decimal TotalDiscounts,
    decimal TotalTaxableBase,
    decimal TotalExclusiveTax,
    decimal TotalInclusiveTax,
    decimal TotalTax,
    decimal TotalLinesPayable);

/// <summary>
/// Centralized pure financial calculation and Pakistan sales tax calculation engine.
/// Adheres strictly to the Financial Rounding Contract (AwayFromZero, 2-decimal precision),
/// Hamilton-Hare largest remainder discount distribution, and statutory sales tax formulas.
/// </summary>
public static class TaxCalculator
{
    private const string PolicyVersion = "1.3.0-AwayFromZero-v1";

    /// <summary>
    /// Executes the complete financial calculation contract across lines, discounts, and taxes.
    /// </summary>
    public static OrderTaxCalculationResult Calculate(
        IReadOnlyList<TaxCalculationLineInput> lines,
        decimal orderDiscountAmount = 0m)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
        {
            return new OrderTaxCalculationResult(
                Array.Empty<LineTaxSnapshot>(),
                Array.Empty<OrderTaxSummarySnapshot>(),
                0.00m, 0.00m, 0.00m, 0.00m, 0.00m, 0.00m, 0.00m, 0.00m, 0.00m);
        }

        // 1. Line Gross Base and Line Discounts
        var grossBases = new decimal[lines.Count];
        var netBeforeOrderDiscs = new decimal[lines.Count];
        decimal totalGross = 0m;
        decimal totalLineDiscounts = 0m;

        for (int i = 0; i < lines.Count; i++)
        {
            var l = lines[i];
            var gross = MoneyRoundingPolicy.RoundMoney(l.Quantity * l.UnitPrice);
            var lineDisc = Math.Min(gross, MoneyRoundingPolicy.RoundMoney(Math.Max(0m, l.LineDiscountAmount)));
            var net = gross - lineDisc;

            grossBases[i] = gross;
            netBeforeOrderDiscs[i] = net;
            totalGross += gross;
            totalLineDiscounts += lineDisc;
        }

        // 2. Order Discount Allocation (Hamilton-Hare / Largest Remainder Method)
        var allocatedOrderDiscs = AllocateOrderDiscount(netBeforeOrderDiscs, orderDiscountAmount);
        decimal totalAllocatedOrderDiscounts = 0m;
        for (int i = 0; i < allocatedOrderDiscs.Length; i++)
        {
            totalAllocatedOrderDiscounts += allocatedOrderDiscs[i];
        }

        // 3. Line Tax Calculation and Snapshot Generation
        var snapshots = new List<LineTaxSnapshot>(lines.Count);
        decimal totalTaxableBase = 0m;
        decimal totalExclusiveTax = 0m;
        decimal totalInclusiveTax = 0m;
        decimal totalLinesPayable = 0m;

        for (int i = 0; i < lines.Count; i++)
        {
            var l = lines[i];
            var gross = grossBases[i];
            var lineDisc = gross - netBeforeOrderDiscs[i];
            var allocatedOrderDisc = allocatedOrderDiscs[i];
            var totalDisc = lineDisc + allocatedOrderDisc;
            var discountedAmount = gross - totalDisc;

            decimal taxableBase;
            decimal taxAmount;
            decimal linePayable;

            bool isTaxable = string.Equals(l.TaxClassification, "Taxable", StringComparison.OrdinalIgnoreCase) &&
                             l.TaxRatePercentage > 0m;

            if (isTaxable)
            {
                if (l.TaxIsInclusive)
                {
                    // Inclusive Tax Extraction: shelf price already includes tax.
                    // PreTaxNet = RoundAwayFromZero(DiscountedGross / (1 + Rate / 100))
                    // InclusiveTax = DiscountedGross - PreTaxNet
                    var preTaxNet = MoneyRoundingPolicy.RoundMoney(discountedAmount / (1m + l.TaxRatePercentage / 100m));
                    taxAmount = discountedAmount - preTaxNet;
                    taxableBase = preTaxNet;
                    linePayable = discountedAmount;
                    totalInclusiveTax += taxAmount;
                }
                else
                {
                    // Exclusive Tax Addition: tax is added on top of discounted base.
                    // ExclusiveTax = RoundAwayFromZero(DiscountedNet * Rate / 100)
                    taxableBase = discountedAmount;
                    taxAmount = MoneyRoundingPolicy.RoundMoney(taxableBase * l.TaxRatePercentage / 100m);
                    linePayable = taxableBase + taxAmount;
                    totalExclusiveTax += taxAmount;
                }
            }
            else
            {
                // Non-taxable, zero-rated, exempt, or out of scope
                taxableBase = discountedAmount;
                taxAmount = 0.00m;
                linePayable = discountedAmount;
            }

            totalTaxableBase += taxableBase;
            totalLinesPayable += linePayable;

            snapshots.Add(new LineTaxSnapshot(
                l.OrderLineId,
                l.TaxClassification,
                l.Authority,
                l.TaxCode,
                l.TaxRatePercentage,
                l.TaxIsInclusive,
                l.Quantity,
                l.UnitPrice,
                gross,
                lineDisc,
                allocatedOrderDisc,
                totalDisc,
                discountedAmount,
                taxableBase,
                taxAmount,
                linePayable,
                PolicyVersion));
        }

        // 4. Statutory Tax Summary Grouping
        var taxSummary = snapshots
            .GroupBy(s => (s.Authority, s.TaxCode, s.TaxRatePercentage, s.TaxClassification, s.TaxIsInclusive))
            .Select(g => new OrderTaxSummarySnapshot(
                g.Key.Authority,
                g.Key.TaxCode,
                g.Key.TaxRatePercentage,
                g.Key.TaxClassification,
                g.Key.TaxIsInclusive,
                g.Sum(x => x.TaxableBase),
                g.Sum(x => x.TaxAmount)))
            .OrderBy(t => t.TaxCode)
            .ToList();

        decimal totalDiscounts = totalLineDiscounts + totalAllocatedOrderDiscounts;
        decimal totalTax = totalExclusiveTax + totalInclusiveTax;

        return new OrderTaxCalculationResult(
            snapshots,
            taxSummary,
            totalGross,
            totalLineDiscounts,
            totalAllocatedOrderDiscounts,
            totalDiscounts,
            totalTaxableBase,
            totalExclusiveTax,
            totalInclusiveTax,
            totalTax,
            totalLinesPayable);
    }

    /// <summary>
    /// Distributes an order-level discount across lines using the Largest Remainder Method (Hamilton-Hare),
    /// guaranteeing penny-exact allocation with zero remainder loss or drift.
    /// </summary>
    public static decimal[] AllocateOrderDiscount(decimal[] lineBases, decimal orderDiscount)
    {
        var result = new decimal[lineBases.Length];
        var roundedOrderDisc = MoneyRoundingPolicy.RoundMoney(Math.Max(0m, orderDiscount));
        if (roundedOrderDisc == 0m || lineBases.Length == 0) return result;

        decimal totalBase = 0m;
        for (int i = 0; i < lineBases.Length; i++)
        {
            totalBase += Math.Max(0m, lineBases[i]);
        }

        if (totalBase == 0m) return result;

        if (roundedOrderDisc >= totalBase)
        {
            // Full discount of all lines up to their maximum
            for (int i = 0; i < lineBases.Length; i++)
            {
                result[i] = Math.Max(0m, lineBases[i]);
            }
            return result;
        }

        var shares = new decimal[lineBases.Length];
        var truncated = new decimal[lineBases.Length];
        var remainders = new (int Index, decimal Remainder)[lineBases.Length];
        decimal allocatedSum = 0m;

        for (int i = 0; i < lineBases.Length; i++)
        {
            var b = Math.Max(0m, lineBases[i]);
            var exactShare = roundedOrderDisc * (b / totalBase);
            shares[i] = exactShare;
            var trunc = Math.Floor(exactShare * 100m) / 100m;
            truncated[i] = trunc;
            remainders[i] = (i, exactShare - trunc);
            allocatedSum += trunc;
        }

        decimal unallocatedCents = MoneyRoundingPolicy.RoundMoney(roundedOrderDisc - allocatedSum);
        int centsCount = Convert.ToInt32(unallocatedCents * 100m);

        // Sort descending by remainder, then by index for determinism
        Array.Sort(remainders, (a, b) =>
        {
            int cmp = b.Remainder.CompareTo(a.Remainder);
            return cmp != 0 ? cmp : a.Index.CompareTo(b.Index);
        });

        for (int i = 0; i < centsCount && i < remainders.Length; i++)
        {
            truncated[remainders[i].Index] += 0.01m;
        }

        for (int i = 0; i < lineBases.Length; i++)
        {
            result[i] = truncated[i];
        }

        return result;
    }
}
