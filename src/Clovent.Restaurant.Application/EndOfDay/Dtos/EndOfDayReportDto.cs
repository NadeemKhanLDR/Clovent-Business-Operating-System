namespace Clovent.Restaurant.Application.EndOfDay.Dtos;

/// <summary>One product's aggregated sales for the report's Items Sold / Top Selling Items section, already ordered by quantity descending.</summary>
public sealed record EndOfDayItemSoldDto(Guid ProductVariantId, decimal Quantity, decimal Total);

/// <summary>One payment method's total collected - the Cash Summary section.</summary>
public sealed record EndOfDayPaymentMethodTotalDto(string PaymentMethodName, decimal Total);

/// <summary>One completed order's receipt row for the report's Bills section, newest first.</summary>
public sealed record EndOfDayBillDto(Guid OrderId, string OrderNumber, DateTimeOffset CompletedAtUtc, decimal Total, string PaymentMethodSummary);

/// <summary>
/// The Day-End / Z-report (a.k.a. Daily Sales Report) for one warehouse over
/// a <see cref="FromDate"/>-<see cref="ToDate"/> range (inclusive, a single
/// day when they're equal) - see <c>GetEndOfDayReportQuery</c> for how each
/// figure is computed and its known limitations (documented there, not
/// repeated per-field here).
/// </summary>
public sealed record EndOfDayReportDto(
    Guid WarehouseId,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal TotalSales,
    decimal CashCollected,
    decimal CardCollected,
    IReadOnlyList<EndOfDayItemSoldDto> ItemsSold,
    IReadOnlyList<EndOfDayPaymentMethodTotalDto> CashSummary,
    IReadOnlyList<EndOfDayBillDto> Bills,
    int ReceiptCount,
    int VoidedOrderCount,
    decimal AverageSale);
