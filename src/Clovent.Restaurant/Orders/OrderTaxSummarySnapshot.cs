namespace Clovent.Restaurant.Orders;

/// <summary>
/// Aggregated statutory tax summary item grouped by tax authority, code, rate, and classification.
/// Rendered on customer receipts, reports, day close snapshots, and tax audits.
/// </summary>
public sealed record OrderTaxSummarySnapshot(
    string Authority,
    string TaxCode,
    decimal RatePercentage,
    string TaxClassification,
    bool TaxIsInclusive,
    decimal TaxableBase,
    decimal TaxAmount);
