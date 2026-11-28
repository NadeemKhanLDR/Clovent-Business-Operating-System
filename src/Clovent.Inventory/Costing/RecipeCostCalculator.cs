using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Inventory.Recipes;
using Clovent.Inventory.UnitsOfMeasure;

namespace Clovent.Inventory.Costing;

/// <summary>Detailed costing breakdown for a single ingredient in a recipe.</summary>
public sealed record IngredientCostBreakdown(
    Guid IngredientVariantId,
    string IngredientName,
    decimal NetQuantity,
    decimal GrossQuantity,
    string RecipeUnit,
    string InventoryUnit,
    decimal UnitCostInInventoryUnit,
    decimal EffectiveUnitCostInRecipeUnit,
    decimal WastagePercentage,
    decimal TotalLineCost);

/// <summary>Comprehensive food costing result for a recipe.</summary>
public sealed record RecipeCostingResult(
    Guid ProductVariantId,
    string RecipeName,
    decimal YieldQuantity,
    string YieldUnit,
    decimal TotalBatchCost,
    decimal CostPerPortion,
    decimal SellingPrice,
    decimal FoodCostPercentage,
    decimal GrossProfitPerPortion,
    decimal GrossMarginPercentage,
    IReadOnlyList<IngredientCostBreakdown> Ingredients);

/// <summary>
/// Domain service that calculates standard theoretical food costs, portion costs,
/// ingredient line breakdowns, and food cost percentages for restaurant recipes.
/// All monetary values are rounded using MidpointRounding.AwayFromZero to 2 decimals,
/// with intermediate unit costs and conversion factors evaluated at 4 decimals.
/// </summary>
public static class RecipeCostCalculator
{
    /// <summary>
    /// Calculates the food cost of a recipe against provided ingredient costs and menu item selling price.
    /// </summary>
    /// <param name="recipe">The recipe aggregate.</param>
    /// <param name="costResolver">Function resolving (UnitCost, UnitOfMeasureCode) for an ingredient variant ID.</param>
    /// <param name="sellingPrice">The selling price of the prepared menu item.</param>
    public static RecipeCostingResult CalculateCost(
        Recipe recipe,
        Func<Guid, (decimal UnitCost, string UnitOfMeasureCode)> costResolver,
        decimal sellingPrice = 0m)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(costResolver);

        var breakdowns = new List<IngredientCostBreakdown>(recipe.Ingredients.Count);
        decimal totalBatchCost = 0m;

        foreach (var ing in recipe.Ingredients)
        {
            var (invCost, invUom) = costResolver(ing.IngredientVariantId);

            // Allow manual standard cost override if defined directly on the recipe line
            decimal effectiveInvCost = ing.CostPerUnitOverride ?? invCost;
            string effectiveInvUom = string.IsNullOrWhiteSpace(invUom) ? ing.UnitOfMeasureCode : invUom;

            decimal grossQty = ing.GrossQuantityRequired;
            decimal lineCost;
            decimal costInRecipeUnit;

            if (string.Equals(ing.UnitOfMeasureCode, effectiveInvUom, StringComparison.OrdinalIgnoreCase))
            {
                costInRecipeUnit = effectiveInvCost;
                lineCost = grossQty * costInRecipeUnit;
            }
            else if (UnitConverter.CanConvert(ing.UnitOfMeasureCode, effectiveInvUom))
            {
                // Convert 1 recipe unit to inventory stock units to determine cost per recipe unit
                // e.g. 1 G of meat when stock is in KG: 1 G = 0.001 KG, so cost is 0.001 * 1200 PKR = 1.20 PKR/G
                decimal conversionFactor = UnitConverter.Convert(1m, ing.UnitOfMeasureCode, effectiveInvUom);
                costInRecipeUnit = Math.Round(effectiveInvCost * conversionFactor, 4, MidpointRounding.AwayFromZero);
                lineCost = grossQty * costInRecipeUnit;
            }
            else
            {
                // Unconvertible units (e.g. PCS to KG without density factor) - fallback to direct multiplication
                costInRecipeUnit = effectiveInvCost;
                lineCost = grossQty * effectiveInvCost;
            }

            lineCost = Math.Round(lineCost, 2, MidpointRounding.AwayFromZero);
            totalBatchCost += lineCost;

            breakdowns.Add(new IngredientCostBreakdown(
                ing.IngredientVariantId,
                ing.IngredientName,
                ing.Quantity,
                grossQty,
                ing.UnitOfMeasureCode,
                effectiveInvUom,
                effectiveInvCost,
                costInRecipeUnit,
                ing.WastagePercentage,
                lineCost));
        }

        totalBatchCost = Math.Round(totalBatchCost, 2, MidpointRounding.AwayFromZero);

        decimal yield = recipe.YieldQuantity > 0m ? recipe.YieldQuantity : 1m;
        decimal costPerPortion = Math.Round(totalBatchCost / yield, 2, MidpointRounding.AwayFromZero);

        decimal foodCostPct = 0m;
        decimal grossProfit = sellingPrice - costPerPortion;
        decimal grossMarginPct = 0m;

        if (sellingPrice > 0m)
        {
            foodCostPct = Math.Round((costPerPortion / sellingPrice) * 100m, 2, MidpointRounding.AwayFromZero);
            grossMarginPct = Math.Round((grossProfit / sellingPrice) * 100m, 2, MidpointRounding.AwayFromZero);
        }

        return new RecipeCostingResult(
            recipe.ProductVariantId,
            recipe.RecipeName,
            recipe.YieldQuantity,
            recipe.YieldUnit,
            totalBatchCost,
            costPerPortion,
            sellingPrice,
            foodCostPct,
            grossProfit,
            grossMarginPct,
            breakdowns);
    }
}
