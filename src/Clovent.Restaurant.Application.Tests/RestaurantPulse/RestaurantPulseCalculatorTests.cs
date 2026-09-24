using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Restaurant.Application.RestaurantPulse;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.RestaurantPulse;

public class RestaurantPulseCalculatorTests
{
    private static RestaurantPulseOrderSample Order(
        decimal total,
        int createdMinutesAgo,
        params (Guid VariantId, string Name, decimal Quantity, decimal Total)[] lines) =>
        new(
            DateTimeOffset.UtcNow.AddMinutes(-createdMinutesAgo),
            DateTimeOffset.UtcNow,
            total,
            [.. lines.Select(l => new RestaurantPulseLineSample(l.VariantId, l.Name, l.Quantity, l.Total))],
            HasBeverageLine: false);

    [Fact]
    public void NormalDay_ComputesAllFigures()
    {
        var burger = Guid.NewGuid();
        var cola = Guid.NewGuid();

        var today = new List<RestaurantPulseOrderSample>
        {
            Order(580m, 30, (burger, "Burger", 1m, 500m), (cola, "Cola", 1m, 80m)) with { HasBeverageLine = true },
            Order(1000m, 15, (burger, "Burger", 2m, 1000m)),
        };

        var result = RestaurantPulseCalculator.Compute(today, yesterdaySales: 790m, beverageUnitPricesToday: [80m], beverageInfoAvailable: true);

        Assert.Equal(1580m, result.TodaySales);
        Assert.Equal(2, result.TodayOrderCount);
        Assert.Equal(790m, result.AverageOrderValue);
        Assert.Equal("Burger", result.BestSellerName);
        Assert.Equal(3m, result.BestSellerQuantity);
        Assert.Equal("Burger", result.HighestRevenueProductName);
        Assert.Equal(1500m, result.HighestRevenueAmount);
        // (1580 - 790) / 790 * 100 = 100%
        Assert.Equal(100m, result.SalesVsYesterdayPercent);
        Assert.Equal(790m, result.YesterdaySales);
        // One beverage-less order * avg beverage price 80.
        Assert.Equal(1, result.OrdersWithoutBeverageCount);
        Assert.Equal(80m, result.BeverageAddOnRevenueOpportunity);
        Assert.True(result.BeverageInfoAvailable);
        // 30 and 15 minutes of service time -> 22.5s average is not the unit here; average of minutes rounds to 1350/2 = 1350s? See below.
        Assert.NotNull(result.AverageOrderTimeSeconds);
        Assert.True(result.AverageOrderTimeSeconds is >= 1300 and <= 1400);
        Assert.False(result.InventoryAvailable);
        Assert.Empty(result.LowStockItems);
    }

    [Fact]
    public void EmptyDay_ReturnsZerosAndNulls_WithoutThrowing()
    {
        var result = RestaurantPulseCalculator.Compute([], yesterdaySales: 0m, beverageUnitPricesToday: [], beverageInfoAvailable: true);

        Assert.Equal(0m, result.TodaySales);
        Assert.Equal(0, result.TodayOrderCount);
        Assert.Equal(0m, result.AverageOrderValue);
        Assert.Null(result.BestSellerName);
        Assert.Equal(0m, result.BestSellerQuantity);
        Assert.Null(result.HighestRevenueProductName);
        Assert.Equal(0m, result.HighestRevenueAmount);
        Assert.Null(result.SalesVsYesterdayPercent);
        Assert.Null(result.AverageOrderTimeSeconds);
        Assert.Equal(0, result.OrdersWithoutBeverageCount);
        Assert.Null(result.BeverageAddOnRevenueOpportunity);
    }

    [Fact]
    public void VsYesterday_ZeroYesterdaySales_IsNullNotInfinite()
    {
        var result = RestaurantPulseCalculator.Compute([Order(500m, 10, (Guid.NewGuid(), "Burger", 1m, 500m))], yesterdaySales: 0m, [], true);

        Assert.Null(result.SalesVsYesterdayPercent);
        Assert.Equal(0m, result.YesterdaySales);
    }

    [Fact]
    public void VsYesterday_NegativeGrowth_ComputesSignedPercent()
    {
        var result = RestaurantPulseCalculator.Compute([Order(500m, 10, (Guid.NewGuid(), "Burger", 1m, 500m))], yesterdaySales: 1000m, [], true);

        Assert.Equal(-50m, result.SalesVsYesterdayPercent);
    }

    [Fact]
    public void ServiceTime_AnomaliesExcluded()
    {
        var good = Order(100m, 10, (Guid.NewGuid(), "Burger", 1m, 100m));
        var negative = new RestaurantPulseOrderSample(
            DateTimeOffset.UtcNow.AddMinutes(-10),
            DateTimeOffset.UtcNow.AddMinutes(-20), // negative elapsed
            100m, [], false);
        var huge = new RestaurantPulseOrderSample(
            DateTimeOffset.UtcNow.AddHours(-5),
            DateTimeOffset.UtcNow, // 5h service time, implausible
            100m, [], false);

        var result = RestaurantPulseCalculator.Compute([good, negative, huge], 0m, [], true);

        // Only the good order's ~10 minutes.
        Assert.NotNull(result.AverageOrderTimeSeconds);
        Assert.True(result.AverageOrderTimeSeconds is >= 570 and <= 630);
    }

    [Fact]
    public void BeverageCategoryUnavailable_FlagFalse_CountStillReported()
    {
        var result = RestaurantPulseCalculator.Compute([Order(100m, 5, (Guid.NewGuid(), "Burger", 1m, 100m))], 0m, [], beverageInfoAvailable: false);

        Assert.False(result.BeverageInfoAvailable);
        Assert.Equal(1, result.OrdersWithoutBeverageCount);
        Assert.Null(result.BeverageAddOnRevenueOpportunity);
    }
}
