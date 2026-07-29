namespace Clovent.Restaurant.Application.Orders;

/// <summary>
/// The computed money figures for one order - never stored on the
/// <c>Order</c> aggregate itself (see its doc comment), always derived by
/// <see cref="OrderTotalsCalculator"/> from its lines/discounts/service
/// charges/payments at read time.
/// </summary>
/// <param name="Subtotal">Sum of every active line's <c>Quantity * UnitPrice</c> - tax-inclusive lines' embedded tax is part of this figure, tax-exclusive lines' tax is not yet added.</param>
/// <param name="TaxTotal">Total tax across every active line, inclusive or exclusive alike - an informational figure for the Tax Summary widget, not an amount to add on top of <paramref name="Subtotal"/> (see <see cref="GrandTotal"/>).</param>
/// <param name="DiscountTotal">Total discount amount, each discount resolved against <paramref name="Subtotal"/>.</param>
/// <param name="ServiceChargeTotal">Total service charge amount, each charge resolved against <paramref name="Subtotal"/>.</param>
/// <param name="GrandTotal">What the customer owes: <paramref name="Subtotal"/> minus <paramref name="DiscountTotal"/> plus <paramref name="ServiceChargeTotal"/> plus only the tax-exclusive lines' tax (tax-inclusive lines' tax is already inside <paramref name="Subtotal"/>).</param>
/// <param name="PaidTotal">Sum of every non-voided payment recorded against the order.</param>
/// <param name="Balance"><paramref name="GrandTotal"/> minus <paramref name="PaidTotal"/> - zero or negative means fully paid.</param>
public sealed record OrderTotals(
    decimal Subtotal,
    decimal TaxTotal,
    decimal DiscountTotal,
    decimal ServiceChargeTotal,
    decimal GrandTotal,
    decimal PaidTotal,
    decimal Balance);
