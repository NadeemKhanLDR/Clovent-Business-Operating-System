using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Inventory.Recipes;

namespace Clovent.Inventory.Application.Recipes.Dtos;

/// <summary>DTO representation of a recipe ingredient line.</summary>
public sealed record RecipeIngredientDto(
    Guid IngredientVariantId,
    string IngredientName,
    decimal Quantity,
    string UnitOfMeasureCode,
    decimal WastagePercentage = 0m,
    decimal? CostPerUnitOverride = null,
    decimal GrossQuantityRequired = 0m)
{
    /// <summary>Projects a domain ingredient to DTO.</summary>
    public static RecipeIngredientDto FromDomain(RecipeIngredient item) => new(
        item.IngredientVariantId,
        item.IngredientName,
        item.Quantity,
        item.UnitOfMeasureCode,
        item.WastagePercentage,
        item.CostPerUnitOverride,
        item.GrossQuantityRequired);
}

/// <summary>DTO representation of a recipe modifier rule.</summary>
public sealed record RecipeModifierDto(
    string ModifierName,
    Guid IngredientVariantId,
    string IngredientName,
    decimal Quantity,
    string UnitOfMeasureCode,
    decimal WastagePercentage = 0m,
    decimal? CostPerUnitOverride = null,
    decimal GrossQuantityRequired = 0m)
{
    /// <summary>Projects a domain modifier to DTO.</summary>
    public static RecipeModifierDto FromDomain(RecipeModifier item) => new(
        item.ModifierName,
        item.IngredientVariantId,
        item.IngredientName,
        item.Quantity,
        item.UnitOfMeasureCode,
        item.WastagePercentage,
        item.CostPerUnitOverride,
        item.GrossQuantityRequired);
}

/// <summary>DTO representation of a historical recipe version.</summary>
public sealed record RecipeVersionDto(
    int VersionNumber,
    DateTimeOffset EffectiveFromUtc,
    decimal YieldQuantity,
    string YieldUnit,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeModifierDto>? Modifiers = null)
{
    /// <summary>Projects a domain version to DTO.</summary>
    public static RecipeVersionDto FromDomain(RecipeVersion v) => new(
        v.VersionNumber,
        v.EffectiveFromUtc,
        v.YieldQuantity,
        v.YieldUnit,
        v.Ingredients.Select(RecipeIngredientDto.FromDomain).ToList(),
        v.Modifiers?.Select(RecipeModifierDto.FromDomain).ToList());
}

/// <summary>Comprehensive DTO representation of a <see cref="Recipe"/> aggregate.</summary>
public sealed record RecipeDto(
    Guid Id,
    Guid ProductVariantId,
    string RecipeName,
    decimal YieldQuantity,
    string YieldUnit,
    int Version,
    bool IsActive,
    string? PreparationNotes,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeModifierDto> Modifiers,
    IReadOnlyList<RecipeVersionDto> Versions,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    /// <summary>Projects a domain recipe aggregate to DTO.</summary>
    public static RecipeDto FromDomain(Recipe r) => new(
        r.Id.Value,
        r.ProductVariantId,
        r.RecipeName,
        r.YieldQuantity,
        r.YieldUnit,
        r.Version,
        r.IsActive,
        r.PreparationNotes,
        r.Ingredients.Select(RecipeIngredientDto.FromDomain).ToList(),
        r.Modifiers.Select(RecipeModifierDto.FromDomain).ToList(),
        r.Versions.Select(RecipeVersionDto.FromDomain).ToList(),
        r.CreatedAtUtc,
        r.UpdatedAtUtc);
}
