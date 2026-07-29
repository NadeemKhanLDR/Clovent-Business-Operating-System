namespace Clovent.Restaurant.Application.EndOfDay.Dtos;

/// <summary>One product's aggregated sales for the report's Items Sold / Top Selling Items section, already ordered by quantity descending.</summary>
public sealed record EndOfDayItemSoldDto(Guid ProductVariantId, decimal Quantity, decimal Total);

/// <summary>One payment method's total collected - the Cash Summary section.</summary>
public sealed record EndOfDayPaymentMethodTotalDto(string PaymentMethodName, decimal Total);

/// <summary>
/// The Day-End / Z-report for one warehouse on one calendar day - see
/// <c>GetEndOfDayReportQuery</c> for how each figure is computed and its
/// known limitations (documented there, not repeated per-field here).
/// </summary>
public sealed record EndOfDayReportDto(
    Guid WarehouseId,
    DateOnly Date,
    decimal TotalSales,
    decimal CashCollected,
    IReadOnlyList<EndOfDayItemSoldDto> ItemsSold,
    IReadOnlyList<EndOfDayPaymentMethodTotalDto> CashSummary,
    int ReceiptCount,
    int VoidedOrderCount,
    decimal AverageSale);
