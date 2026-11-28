using System;
using System.Collections.Generic;
using Clovent.Inventory.Costing;
using Clovent.Inventory.Recipes;
using Xunit;

namespace Clovent.Inventory.Tests.Costing;

public class RecipeCostCalculatorTests
{
    [Fact]
    public void CalculateCost_StandardBurger_CalculatesCorrectFoodCostAndMargin()
    {
        var beefId = Guid.NewGuid();
        var bunId = Guid.NewGuid();
        var sauceId = Guid.NewGuid();

        var recipe = Recipe.Create(RecipeId.New(), Guid.NewGuid(), "Beef Burger", 1m, "PORTION");

        // 150g beef with 10% trim loss = 0.1650 KG gross
        recipe.AddOrUpdateIngredient(beefId, "Beef Mince", 0.1500m, "KG", 10m);

        // 1 bun with 0% loss = 1.0000 PCS gross
        recipe.AddOrUpdateIngredient(bunId, "Brioche Bun", 1m, "PCS", 0m);

        // 30ml sauce with 5% loss = 31.5 ML gross
        recipe.AddOrUpdateIngredient(sauceId, "Special Sauce", 30m, "ML", 5m);

        // Unit costs:
        // Beef: 1200 PKR / KG -> 0.1650 * 1200 = 198.00 PKR
        // Bun: 50 PKR / PCS -> 1 * 50 = 50.00 PKR
        // Sauce: 1000 PKR / L -> 31.5 ML at 1000/1000 = 0.0315 * 1000 = 31.50 PKR
        // Total Recipe Cost = 198.00 + 50.00 + 31.50 = 279.50 PKR
        var costLookup = new Dictionary<Guid, (decimal UnitCost, string UnitOfMeasureCode)>
        {
            [beefId] = (1200m, "KG"),
            [bunId] = (50m, "PCS"),
            [sauceId] = (1000m, "L")
        };

        decimal sellingPrice = 750.00m;

        var result = RecipeCostCalculator.CalculateCost(
            recipe,
            vid => costLookup.GetValueOrDefault(vid, (0m, "")),
            sellingPrice);

        Assert.Equal(279.50m, result.TotalBatchCost);
        Assert.Equal(279.50m, result.CostPerPortion);
        Assert.Equal(750.00m, result.SellingPrice);
        Assert.Equal(470.50m, result.GrossProfitPerPortion); // 750 - 279.50

        // Food Cost % = (279.50 / 750) * 100 = 37.27%
        Assert.Equal(37.27m, result.FoodCostPercentage);

        // Gross Margin % = (470.50 / 750) * 100 = 62.73%
        Assert.Equal(62.73m, result.GrossMarginPercentage);
    }

    [Fact]
    public void CalculateCost_WithIngredientCostOverride_PrefersOverride()
    {
        var chickenId = Guid.NewGuid();
        var recipe = Recipe.Create(RecipeId.New(), Guid.NewGuid(), "Crispy Tender", 1m, "PORTION");

        // Override cost = 500 PKR / KG instead of catalog 600 PKR / KG
        recipe.AddOrUpdateIngredient(chickenId, "Chicken Fillet", 0.2000m, "KG", 0m, 500m);

        var result = RecipeCostCalculator.CalculateCost(
            recipe,
            _ => (600m, "KG"), // Catalog returns 600
            200m);

        // Should use override 500 -> 0.2000 * 500 = 100 PKR
        Assert.Equal(100.00m, result.CostPerPortion);
    }

    [Fact]
    public void CalculateCost_ZeroSellingPrice_AvoidsDivideByZero()
    {
        var recipe = Recipe.Create(RecipeId.New(), Guid.NewGuid(), "Sample Item", 1m, "PORTION");
        recipe.AddOrUpdateIngredient(Guid.NewGuid(), "Ingredient", 1m, "PCS", 0m, 10m);

        var result = RecipeCostCalculator.CalculateCost(recipe, _ => (10m, "PCS"), 0m);

        Assert.Equal(10m, result.CostPerPortion);
        Assert.Equal(0m, result.FoodCostPercentage);
        Assert.Equal(0m, result.GrossMarginPercentage);
    }
}
