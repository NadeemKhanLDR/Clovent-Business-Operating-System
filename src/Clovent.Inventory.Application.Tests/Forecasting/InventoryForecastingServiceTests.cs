using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.Forecasting.Services;
using Clovent.Inventory.Application.Tests.TestSupport;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Inventory.Application.Tests.Forecasting;

public sealed class InventoryForecastingServiceTests
{
    private readonly FakeWarehouseStockRepository _stockRepo = new();
    private readonly FakeInventoryTransactionRepository _txRepo = new();
    private readonly InventoryForecastingService _service;

    private readonly WarehouseId _warehouseId = WarehouseId.New();
    private readonly ProductVariantId _biryaniVariantId = ProductVariantId.New();
    private readonly ProductVariantId _naanVariantId = ProductVariantId.New();

    public InventoryForecastingServiceTests()
    {
        _service = new InventoryForecastingService(
            _stockRepo,
            _txRepo,
            NullLogger<InventoryForecastingService>.Instance);
    }

    [Fact]
    public async Task CalculateBurnRates_ComputesAccurateHourlyVelocityFromHistory()
    {
        // Arrange: 100 units on hand
        var stock = WarehouseStock.Create(_warehouseId, _biryaniVariantId, minimumStock: 10, maximumStock: 200);
        stock.Receive(100m);
        await _stockRepo.AddAsync(stock);

        var now = DateTimeOffset.UtcNow;

        // Seed 100 units issued evenly over the last 50 hours (2 units/hour)
        for (int i = 50; i >= 1; i -= 5)
        {
            var tx = InventoryTransaction.Create(
                _warehouseId,
                _biryaniVariantId,
                InventoryTransactionType.Issue,
                10m,
                occurredAtUtc: now.AddHours(-i));
            await _txRepo.AddAsync(tx);
        }

        // Act
        var result = await _service.CalculateBurnRatesAndDepletionAsync(
            _warehouseId.Value,
            nextScheduledGrnUtc: now.AddHours(24),
            observationHours: 168);

        // Assert
        var item = Assert.Single(result);
        Assert.Equal(stock.ProductVariantId.Value, item.ProductVariantId);
        Assert.True(item.BaseHourlyBurnRate >= 1.8m && item.BaseHourlyBurnRate <= 2.2m,
            $"Expected ~2.0/hr, got {item.BaseHourlyBurnRate}");
    }

    [Fact]
    public async Task CalculateBurnRates_WhenStockDepletesBeforeScheduledGrn_SetsIsDepletedBeforeGrnTrue()
    {
        // Arrange: Only 20 units on hand
        var stock = WarehouseStock.Create(_warehouseId, _naanVariantId, minimumStock: 15, maximumStock: 300);
        stock.Receive(20m);
        await _stockRepo.AddAsync(stock);

        var now = DateTimeOffset.UtcNow;

        // Burn rate of 2 units/hour over last 24 hours -> 20 units will deplete in ~10 hours
        var tx = InventoryTransaction.Create(
            _warehouseId,
            _naanVariantId,
            InventoryTransactionType.Issue,
            48m,
            occurredAtUtc: now.AddHours(-24));
        await _txRepo.AddAsync(tx);

        // Scheduled GRN delivery is in 24 hours (10 hours < 24 hours)
        var scheduledGrnTime = now.AddHours(24);

        // Act
        var result = await _service.CalculateBurnRatesAndDepletionAsync(
            _warehouseId.Value,
            nextScheduledGrnUtc: scheduledGrnTime,
            observationHours: 48);

        // Assert
        var item = Assert.Single(result);
        Assert.True(item.IsDepletedBeforeGrn, "Stock should deplete before GRN arrival");
        Assert.True(item.HoursUntilDepletion < item.HoursUntilGrn,
            $"Depletion {item.HoursUntilDepletion}h must be less than GRN {item.HoursUntilGrn}h");
        Assert.True(item.RecommendedReorderQuantity > 0, "Reorder recommendation must be positive");
        Assert.Contains(item.Urgency, new[] { "Critical", "High" });
    }

    [Fact]
    public async Task CalculateBurnRates_WhenStockSufficientForGrn_SetsIsDepletedBeforeGrnFalse()
    {
        // Arrange: 200 units on hand (ample stock)
        var stock = WarehouseStock.Create(_warehouseId, _biryaniVariantId, minimumStock: 20, maximumStock: 500);
        stock.Receive(200m);
        await _stockRepo.AddAsync(stock);

        var now = DateTimeOffset.UtcNow;

        // Burn rate of 1 unit/hour (will last 200 hours)
        var tx = InventoryTransaction.Create(
            _warehouseId,
            _biryaniVariantId,
            InventoryTransactionType.Issue,
            24m,
            occurredAtUtc: now.AddHours(-24));
        await _txRepo.AddAsync(tx);

        // Next GRN delivery is in 24 hours (ample time)
        var scheduledGrnTime = now.AddHours(24);

        // Act
        var result = await _service.CalculateBurnRatesAndDepletionAsync(
            _warehouseId.Value,
            nextScheduledGrnUtc: scheduledGrnTime);

        // Assert
        var item = Assert.Single(result);
        Assert.False(item.IsDepletedBeforeGrn);
        Assert.True(item.HoursUntilDepletion > item.HoursUntilGrn);
        Assert.Equal("Adequate", item.Urgency);
    }

    [Fact]
    public async Task CalculateBurnRates_WhenStockAlreadyExhausted_SurfacesImmediateCriticalStatus()
    {
        // Arrange: 0 units available
        var stock = WarehouseStock.Create(_warehouseId, _naanVariantId, minimumStock: 10, maximumStock: 200);
        await _stockRepo.AddAsync(stock);

        var now = DateTimeOffset.UtcNow;
        var tx = InventoryTransaction.Create(
            _warehouseId,
            _naanVariantId,
            InventoryTransactionType.Issue,
            10m,
            occurredAtUtc: now.AddHours(-5));
        await _txRepo.AddAsync(tx);

        // Act
        var result = await _service.CalculateBurnRatesAndDepletionAsync(
            _warehouseId.Value,
            nextScheduledGrnUtc: now.AddHours(12));

        // Assert
        var item = Assert.Single(result);
        Assert.Equal(0m, item.QuantityAvailable);
        Assert.Equal(0m, item.HoursUntilDepletion);
        Assert.True(item.IsDepletedBeforeGrn);
        Assert.Equal("Critical", item.Urgency);
    }

    [Fact]
    public async Task GenerateStockoutAlerts_SurfacesOnlyAtRiskItemsWithDetailedSummary()
    {
        // Arrange: 1 critical item and 1 adequate item
        var criticalStock = WarehouseStock.Create(_warehouseId, _naanVariantId, minimumStock: 10, maximumStock: 200);
        criticalStock.Receive(5m); // 5 on hand, depletes fast
        await _stockRepo.AddAsync(criticalStock);

        var safeStock = WarehouseStock.Create(_warehouseId, _biryaniVariantId, minimumStock: 10, maximumStock: 200);
        safeStock.Receive(150m);
        await _stockRepo.AddAsync(safeStock);

        var now = DateTimeOffset.UtcNow;
        await _txRepo.AddAsync(InventoryTransaction.Create(_warehouseId, _naanVariantId, InventoryTransactionType.Issue, 20m, occurredAtUtc: now.AddHours(-10)));
        await _txRepo.AddAsync(InventoryTransaction.Create(_warehouseId, _biryaniVariantId, InventoryTransactionType.Issue, 10m, occurredAtUtc: now.AddHours(-10)));

        // Act
        var alerts = await _service.GenerateStockoutAlertsAsync(_warehouseId.Value, nextScheduledGrnUtc: now.AddHours(24));

        // Assert
        var alert = Assert.Single(alerts);
        Assert.Equal(_naanVariantId.Value, alert.ProductVariantId);
        Assert.True(alert.IsDepletedBeforeGrn);
        Assert.Contains("Stockout", alert.RiskSummary);
    }

    [Fact]
    public async Task GenerateReorderRecommendations_GeneratesPurchaseSuggestionsWithPriority()
    {
        // Arrange: Low stock item
        var stock = WarehouseStock.Create(_warehouseId, _naanVariantId, minimumStock: 20, maximumStock: 100);
        stock.Receive(8m);
        await _stockRepo.AddAsync(stock);

        var now = DateTimeOffset.UtcNow;
        await _txRepo.AddAsync(InventoryTransaction.Create(_warehouseId, _naanVariantId, InventoryTransactionType.Issue, 24m, occurredAtUtc: now.AddHours(-12)));

        // Act
        var reorders = await _service.GenerateReorderRecommendationsAsync(_warehouseId.Value, nextScheduledGrnUtc: now.AddHours(24));

        // Assert
        var recommendation = Assert.Single(reorders);
        Assert.True(recommendation.SuggestedReorderQuantity >= 20m);
        Assert.Contains(recommendation.Priority, new[] { "Emergency", "High" });
        Assert.NotEmpty(recommendation.Justification);
    }
}
