using System;

namespace Clovent.Inventory.Recipes;

/// <summary>
/// An ingredient line in a restaurant recipe specifying an inventory product variant,
/// required quantity, unit of measure, trim/prep wastage percentage, and standard cost.
/// </summary>
public sealed record RecipeIngredient(
    Guid IngredientVariantId,
    string IngredientName,
    decimal Quantity,
    string UnitOfMeasureCode,
    decimal WastagePercentage = 0m,
    decimal? CostPerUnitOverride = null)
{
    /// <summary>
    /// Computes the gross quantity required including preparation trim loss.
    /// </summary>
    public decimal GrossQuantityRequired =>
        Math.Round(Quantity * (1m + (WastagePercentage / 100m)), 4, MidpointRounding.AwayFromZero);
}

/// <summary>
/// A modifier ingredient definition that consumes additional stock when an order line
/// has an active modifier (e.g. "+ Extra Cheese Slice", "+ Extra Patty").
/// </summary>
public sealed record RecipeModifier(
    string ModifierName,
    Guid IngredientVariantId,
    string IngredientName,
    decimal Quantity,
    string UnitOfMeasureCode,
    decimal WastagePercentage = 0m,
    decimal? CostPerUnitOverride = null)
{
    /// <summary>
    /// Computes the gross quantity required including preparation trim loss.
    /// </summary>
    public decimal GrossQuantityRequired =>
        Math.Round(Quantity * (1m + (WastagePercentage / 100m)), 4, MidpointRounding.AwayFromZero);
}

/// <summary>
/// An immutable historical snapshot of a recipe version.
/// </summary>
public sealed record RecipeVersion(
    int VersionNumber,
    DateTimeOffset EffectiveFromUtc,
    decimal YieldQuantity,
    string YieldUnit,
    IReadOnlyList<RecipeIngredient> Ingredients,
    IReadOnlyList<RecipeModifier>? Modifiers = null);
