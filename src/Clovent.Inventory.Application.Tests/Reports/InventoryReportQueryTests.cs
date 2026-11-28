using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.Reports.Queries;
using Clovent.Inventory.Application.Tests.TestSupport;
using Clovent.Inventory.Recipes;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using Xunit;

namespace Clovent.Inventory.Application.Tests.Reports;

public class InventoryReportQueryTests
{
    private readonly FakeRecipeRepository _recipeRepo = new();
    private readonly FakeWarehouseStockRepository _stockRepo = new();
    private readonly FakeInventoryTransactionRepository _txRepo = new();
    private readonly FakeMediator _mediator;

    public InventoryReportQueryTests()
    {
        _mediator = new FakeMediator(req =>
        {
            if (req is GetProductVariantByIdQuery q)
            {
                return Task.FromResult<object?>(new ProductVariantDto(
                    q.ProductVariantId, Guid.NewGuid(), "Sample Item", "SKU-TEST", Guid.Empty, "Active", 1, DateTimeOffset.UtcNow));
            }

            if (req is ListProductPricesByVariantQuery pq)
            {
                return Task.FromResult<object?>(new List<ProductPriceDto>
                {
                    new(Guid.NewGuid(), pq.ProductVariantId, nameof(PriceType.Cost), 200m, Guid.NewGuid(), DateTimeOffset.UtcNow, "Active", DateTimeOffset.UtcNow),
                    new(Guid.NewGuid(), pq.ProductVariantId, nameof(PriceType.Selling), 600m, Guid.NewGuid(), DateTimeOffset.UtcNow, "Active", DateTimeOffset.UtcNow)
                });
            }

            return Task.FromResult<object?>(null);
        });
    }

    [Fact]
    public async Task GetInventoryValuationReport_CalculatesTotalValuationAndAlerts()
    {
        var warehouseId = WarehouseId.New();
        var variant1 = ProductVariantId.New();
        var variant2 = ProductVariantId.New();

        // 10 units on hand, min 5 -> Normal
        var stock1 = WarehouseStock.Create(warehouseId, variant1, 5m, 50m, false);
        stock1.Receive(10m);

        // 2 units on hand, min 5 -> Low Stock
        var stock2 = WarehouseStock.Create(warehouseId, variant2, 5m, 50m, false);
        stock2.Receive(2m);

        _stockRepo.Add(stock1);
        _stockRepo.Add(stock2);

        var handler = new GetInventoryValuationReportQueryHandler(_stockRepo, _mediator);
        var report = await handler.Handle(new GetInventoryValuationReportQuery(warehouseId.Value), CancellationToken.None);

        Assert.Equal(2, report.TotalItems);
        Assert.Equal(1, report.LowStockCount);
        Assert.Equal(0, report.NegativeStockCount);
        // Total valuation: 10 * 200 + 2 * 200 = 2400 PKR
        Assert.Equal(2400.00m, report.TotalAssetValuation);
    }

    [Fact]
    public async Task GetIngredientCostReport_AggregatesUsageAndValuation()
    {
        var ingredientId = Guid.NewGuid();
        var recipe1 = Recipe.Create(RecipeId.New(), Guid.NewGuid(), "Burger", 1m, "PORTION");
        recipe1.AddOrUpdateIngredient(ingredientId, "Onion", 0.0500m, "KG", 0m);

        var recipe2 = Recipe.Create(RecipeId.New(), Guid.NewGuid(), "Salad", 1m, "PORTION");
        recipe2.AddOrUpdateIngredient(ingredientId, "Onion", 0.1000m, "KG", 0m);

        await _recipeRepo.AddAsync(recipe1);
        await _recipeRepo.AddAsync(recipe2);

        var stock = WarehouseStock.Create(WarehouseId.New(), new ProductVariantId(ingredientId), 10m, 100m, false);
        stock.Receive(50m);
        _stockRepo.Add(stock);

        var handler = new GetIngredientCostReportQueryHandler(_recipeRepo, _stockRepo, _mediator);
        var report = await handler.Handle(new GetIngredientCostReportQuery(), CancellationToken.None);

        Assert.Equal(1, report.TotalIngredients);
        var item = Assert.Single(report.Items);
        Assert.Equal(2, item.RecipesUsedInCount);
        Assert.Equal(50m, item.QuantityOnHand);
        Assert.Equal(200m, item.CurrentUnitCost);
        Assert.Equal(10000.00m, item.TotalStockValuation); // 50 * 200
    }

    [Fact]
    public async Task GetStockVarianceReport_CalculatesVarianceCorrectly()
    {
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var now = DateTimeOffset.UtcNow;

        // Sales issue = 10 units
        var saleTx = InventoryTransaction.Create(
            warehouseId, variantId, InventoryTransactionType.Issue, 10m, "Order", Guid.NewGuid(), "Order #101", now);

        // Wastage adjustment = 2 units
        var wasteTx = InventoryTransaction.Create(
            warehouseId, variantId, InventoryTransactionType.Adjustment, 2m, "Waste", Guid.NewGuid(), "Spilled milk (waste)", now);

        _txRepo.Add(saleTx);
        _txRepo.Add(wasteTx);

        var handler = new GetStockVarianceReportQueryHandler(_txRepo, _mediator);
        var report = await handler.Handle(
            new GetStockVarianceReportQuery(warehouseId.Value, now.AddHours(-1), now.AddHours(1)),
            CancellationToken.None);

        Assert.Single(report.Items);
        var item = report.Items[0];
        Assert.Equal(10m, item.TheoreticalQuantity);
        Assert.Equal(12m, item.ActualQuantity);
        Assert.Equal(2m, item.VarianceQuantity);
        Assert.Equal(400.00m, item.VarianceCost); // 2 * 200
        Assert.Equal("Shrinkage / Waste", item.VarianceClassification);
    }

    [Fact]
    public async Task GetWastageReport_SummarizesKitchenWastageIncidents()
    {
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var now = DateTimeOffset.UtcNow;

        var wasteTx = InventoryTransaction.Create(
            warehouseId, variantId, InventoryTransactionType.Adjustment, 3m, "Wastage", Guid.NewGuid(), "Spoiled during prep", now);
        _txRepo.Add(wasteTx);

        var handler = new GetWastageReportQueryHandler(_txRepo, _mediator);
        var report = await handler.Handle(new GetWastageReportQuery(warehouseId.Value), CancellationToken.None);

        Assert.Equal(1, report.TotalIncidents);
        Assert.Equal(3m, report.TotalWastageQuantity);
        Assert.Equal(600.00m, report.TotalWastageCost); // 3 * 200
    }
}
