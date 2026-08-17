using Clovent.Desktop.Dashboard;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Tables.Dtos;
using Xunit;

namespace Clovent.Desktop.Tests.Dashboard;

public class RestaurantDashboardCalculationsTests
{
    private static TableDto CreateTable(string occupancyStatus) => new(
        Guid.NewGuid(), Guid.NewGuid(), "T-01", 4, "Active", occupancyStatus, DateTimeOffset.UtcNow);

    private static OrderDto CreateOrder(string status, DateTimeOffset updatedAtUtc) => new(
        Guid.NewGuid(), "ORD-1", null, "DineIn", status, Guid.NewGuid(), Guid.NewGuid(), null, null, [], [], [], [], DateTimeOffset.UtcNow, updatedAtUtc, null);

    private static OrderLineDto CreateLine(Guid variantId, decimal quantity, bool isVoided = false) => new(
        Guid.NewGuid(), Guid.NewGuid(), variantId, quantity, 9.99m, 9.99m, false, null, null, null, 0m, false, null, isVoided, quantity * 9.99m, DateTimeOffset.UtcNow);

    [Fact]
    public void CountOccupiedTables_CountsOnlyOccupied()
    {
        TableDto[] tables =
        [
            CreateTable("Occupied"),
            CreateTable("Available"),
            CreateTable("Occupied"),
            CreateTable("Reserved"),
            CreateTable("OutOfService"),
        ];

        Assert.Equal(2, RestaurantDashboardCalculations.CountOccupiedTables(tables));
    }

    [Fact]
    public void FilterCompletedOn_MatchesCompletedOrdersOnGivenDate()
    {
        var today = new DateOnly(2026, 7, 27);
        var todayUtc = new DateTimeOffset(2026, 7, 27, 14, 0, 0, TimeSpan.Zero);
        var yesterdayUtc = new DateTimeOffset(2026, 7, 26, 14, 0, 0, TimeSpan.Zero);

        OrderDto[] orders =
        [
            CreateOrder("Completed", todayUtc),
            CreateOrder("Completed", yesterdayUtc),
            CreateOrder("Open", todayUtc),
            CreateOrder("Voided", todayUtc),
        ];

        var result = RestaurantDashboardCalculations.FilterCompletedOn(orders, today);

        var single = Assert.Single(result);
        Assert.Equal("Completed", single.Status);
        Assert.Equal(todayUtc, single.UpdatedAtUtc);
    }

    [Fact]
    public void TopSellingItems_RanksByTotalQuantityDescending_ExcludingVoidedLines()
    {
        var popular = Guid.NewGuid();
        var lessPopular = Guid.NewGuid();
        var voidedOnly = Guid.NewGuid();

        OrderLineDto[] lines =
        [
            CreateLine(popular, 3),
            CreateLine(popular, 4),
            CreateLine(lessPopular, 2),
            CreateLine(voidedOnly, 100, isVoided: true),
        ];

        var result = RestaurantDashboardCalculations.TopSellingItems(lines, top: 5);

        Assert.Equal(2, result.Count);
        Assert.Equal(popular, result[0].ProductVariantId);
        Assert.Equal(7m, result[0].Quantity);
        Assert.Equal(lessPopular, result[1].ProductVariantId);
        Assert.Equal(2m, result[1].Quantity);
    }

    [Fact]
    public void TopSellingItems_RespectsTopLimit()
    {
        OrderLineDto[] lines = [.. Enumerable.Range(0, 10).Select(i => CreateLine(Guid.NewGuid(), i + 1))];

        var result = RestaurantDashboardCalculations.TopSellingItems(lines, top: 3);

        Assert.Equal(3, result.Count);
    }
}
