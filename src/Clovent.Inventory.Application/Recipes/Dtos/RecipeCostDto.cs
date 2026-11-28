using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Inventory.Costing;

namespace Clovent.Inventory.Application.Recipes.Dtos;

/// <summary>DTO representation of an ingredient cost breakdown line.</summary>
public sealed record IngredientCostBreakdownDto(
    Guid IngredientVariantId,
    string IngredientName,
    decimal NetQuantity,
    decimal GrossQuantity,
    string RecipeUnit,
    string InventoryUnit,
    decimal UnitCostInInventoryUnit,
    decimal EffectiveUnitCostInRecipeUnit,
    decimal WastagePercentage,
    decimal TotalLineCost)
{
    /// <summary>Projects domain breakdown to DTO.</summary>
    public static IngredientCostBreakdownDto FromDomain(IngredientCostBreakdown b) => new(
        b.IngredientVariantId,
        b.IngredientName,
        b.NetQuantity,
        b.GrossQuantity,
        b.RecipeUnit,
        b.InventoryUnit,
        b.UnitCostInInventoryUnit,
        b.EffectiveUnitCostInRecipeUnit,
        b.WastagePercentage,
        b.TotalLineCost);
}

/// <summary>DTO representation of a full recipe food costing report.</summary>
public sealed record RecipeCostDto(
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
    IReadOnlyList<IngredientCostBreakdownDto> Ingredients)
{
    /// <summary>Projects domain costing result to DTO.</summary>
    public static RecipeCostDto FromDomain(RecipeCostingResult r) => new(
        r.ProductVariantId,
        r.RecipeName,
        r.YieldQuantity,
        r.YieldUnit,
        r.TotalBatchCost,
        r.CostPerPortion,
        r.SellingPrice,
        r.FoodCostPercentage,
        r.GrossProfitPerPortion,
        r.GrossMarginPercentage,
        r.Ingredients.Select(IngredientCostBreakdownDto.FromDomain).ToList());
}
