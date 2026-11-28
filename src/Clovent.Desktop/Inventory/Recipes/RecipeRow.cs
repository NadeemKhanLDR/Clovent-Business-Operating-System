using System;

namespace Clovent.Desktop.Inventory.Recipes;

/// <summary>
/// Display row model for the Recipe Management back-office grid.
/// Enriches the recipe aggregate with catalog variant names and theoretical costing metrics.
/// </summary>
public sealed record RecipeRow(
    Guid RecipeId,
    string RecipeName,
    Guid ProductVariantId,
    string ProductVariantName,
    decimal YieldQuantity,
    string YieldUnit,
    int Version,
    bool IsActive,
    int IngredientCount,
    int ModifierCount,
    decimal CostPerPortion,
    decimal SellingPrice,
    decimal FoodCostPercentage,
    decimal GrossMarginPercentage,
    bool IsAlert)
{
    /// <summary>Formatted yield string (e.g. "1.0000 Portion").</summary>
    public string YieldDisplay => $"{YieldQuantity:0.####} {YieldUnit}";

    /// <summary>Active status display.</summary>
    public string Status => IsActive ? "Active" : "Inactive";

    /// <summary>Formatted theoretical food cost percentage.</summary>
    public string FoodCostDisplay => $"{FoodCostPercentage:0.##}%";

    /// <summary>Formatted theoretical gross margin percentage.</summary>
    public string GrossMarginDisplay => $"{GrossMarginPercentage:0.##}%";
}
