using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Orders;

/// <summary>
/// The computed money figures for one order - derived by <see cref="OrderTotalsCalculator"/>
/// from its lines, discounts, service charges, and payments.
/// Enhanced with statutory Pakistan sales tax breakdowns while preserving full backward compatibility.
/// </summary>
/// <param name="Subtotal">Sum of every active line's gross <c>Quantity * UnitPrice</c>.</param>
/// <param name="TaxTotal">Total statutory sales tax (inclusive + exclusive) across active lines.</param>
/// <param name="DiscountTotal">Total discount amount (line discounts plus allocated order discounts).</param>
/// <param name="ServiceChargeTotal">Total service charge amount.</param>
/// <param name="GrandTotal">Customer payable total: <paramref name="Subtotal"/> minus <paramref name="DiscountTotal"/> plus <paramref name="ServiceChargeTotal"/> plus <paramref name="ExclusiveTaxTotal"/>.</param>
/// <param name="PaidTotal">Sum of non-voided payments.</param>
/// <param name="Balance"><paramref name="GrandTotal"/> minus <paramref name="PaidTotal"/>.</param>
/// <param name="ExclusiveTaxTotal">Statutory tax amount added on top of bill for tax-exclusive lines.</param>
/// <param name="InclusiveTaxTotal">Statutory tax amount embedded inside shelf prices for tax-inclusive lines.</param>
/// <param name="TaxableBaseTotal">Net taxable base across all lines.</param>
/// <param name="LineTaxSnapshots">Per-line immutable tax snapshots.</param>
/// <param name="TaxSummary">Statutory tax summary grouped by authority, tax code, and rate.</param>
public sealed record OrderTotals(
    decimal Subtotal,
    decimal TaxTotal,
    decimal DiscountTotal,
    decimal ServiceChargeTotal,
    decimal GrandTotal,
    decimal PaidTotal,
    decimal Balance,
    decimal ExclusiveTaxTotal = 0m,
    decimal InclusiveTaxTotal = 0m,
    decimal TaxableBaseTotal = 0m,
    IReadOnlyList<LineTaxSnapshot>? LineTaxSnapshots = null,
    IReadOnlyList<OrderTaxSummarySnapshot>? TaxSummary = null);
