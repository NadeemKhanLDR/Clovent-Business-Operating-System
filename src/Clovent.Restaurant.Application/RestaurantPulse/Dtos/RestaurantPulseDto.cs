namespace Clovent.Restaurant.Application.RestaurantPulse.Dtos;

/// <summary>
/// The Restaurant Pulse panel's read model: today's headline sales figures,
/// best sellers, service speed, and beverage upsell opportunity. Nullable
/// fields are <see langword="null"/> (never exceptions) when the underlying
/// data cannot be computed - e.g. sales vs yesterday when yesterday sold
/// nothing, or the beverage opportunity when no drink category is
/// identifiable.
/// </summary>
public sealed record RestaurantPulseDto(
    decimal TodaySales,
    int TodayOrderCount,
    decimal AverageOrderValue,
    string? BestSellerName,
    decimal BestSellerQuantity,
    string? HighestRevenueProductName,
    decimal HighestRevenueAmount,
    decimal? SalesVsYesterdayPercent,
    decimal YesterdaySales,
    int? AverageOrderTimeSeconds,
    int OrdersWithoutBeverageCount,
    decimal? BeverageAddOnRevenueOpportunity,
    bool BeverageInfoAvailable,
    bool InventoryAvailable,
    IReadOnlyCollection<RestaurantPulseLowStockItemDto> LowStockItems);
