using System;
using System.Linq;
using Clovent.Inventory.Recipes;
using Xunit;

namespace Clovent.Inventory.Tests.Recipes;

public class RecipeTests
{
    [Fact]
    public void Create_ValidParameters_InitializesCorrectly()
    {
        var recipeId = RecipeId.New();
        var productVariantId = Guid.NewGuid();

        var recipe = Recipe.Create(
            recipeId,
            productVariantId,
            "Classic Beef Burger",
            1m,
            "PORTION",
            "Signature 150g beef patty with brioche bun");

        Assert.Equal(recipeId, recipe.Id);
        Assert.Equal(productVariantId, recipe.ProductVariantId);
        Assert.Equal("Classic Beef Burger", recipe.RecipeName);
        Assert.Equal(1m, recipe.YieldQuantity);
        Assert.Equal("PORTION", recipe.YieldUnit);
        Assert.True(recipe.IsActive);
        Assert.Equal(1, recipe.VersionNumber);
        Assert.Empty(recipe.Ingredients);
        Assert.Empty(recipe.Modifiers);
        Assert.Empty(recipe.VersionHistory);
    }

    [Fact]
    public void AddOrUpdateIngredient_CalculatesGrossQuantityWithWastage()
    {
        var recipe = Recipe.Create(RecipeId.New(), Guid.NewGuid(), "Fries", 1m, "PORTION");
        var potatoId = Guid.NewGuid();

        // 200g net with 15% peel/trim wastage
        recipe.AddOrUpdateIngredient(potatoId, "Raw Potatoes", 0.2000m, "KG", 15m, 120m);

        Assert.Single(recipe.Ingredients);
        var ing = recipe.Ingredients.First();
        Assert.Equal(potatoId, ing.IngredientVariantId);
        Assert.Equal(0.2000m, ing.Quantity);
        Assert.Equal(15m, ing.WastagePercentage);
        // Gross = 0.2000 * 1.15 = 0.2300 KG
        Assert.Equal(0.2300m, ing.GrossQuantityRequired);
        Assert.Equal(120m, ing.CostPerUnitOverride);
    }

    [Fact]
    public void AddModifier_StoresModifierDefinition()
    {
        var recipe = Recipe.Create(RecipeId.New(), Guid.NewGuid(), "Burger", 1m, "PORTION");
        var cheeseId = Guid.NewGuid();

        recipe.AddModifier("Extra Cheddar", cheeseId, "Cheddar Cheese Slice", 1m, "SLICE", 0m, 45m);

        Assert.Single(recipe.Modifiers);
        var mod = recipe.Modifiers.First();
        Assert.Equal("Extra Cheddar", mod.ModifierName);
        Assert.Equal(cheeseId, mod.IngredientVariantId);
        Assert.Equal(1m, mod.Quantity);
        Assert.Equal("SLICE", mod.UnitOfMeasureCode);
    }

    [Fact]
    public void CreateNewVersion_ArchivesCurrentStateAndIncrementsVersion()
    {
        var recipe = Recipe.Create(RecipeId.New(), Guid.NewGuid(), "Pizza", 8m, "SLICE");
        recipe.AddOrUpdateIngredient(Guid.NewGuid(), "Flour", 0.5000m, "KG", 5m);

        recipe.CreateNewVersion(8m, "SLICE");

        Assert.Equal(2, recipe.VersionNumber);
        Assert.Single(recipe.VersionHistory);
        var historical = recipe.VersionHistory.First();
        Assert.Equal(1, historical.VersionNumber);
        Assert.Single(historical.Ingredients);
    }

    [Fact]
    public void DeactivateAndReactivate_TogglesActiveFlag()
    {
        var recipe = Recipe.Create(RecipeId.New(), Guid.NewGuid(), "Soup", 1m, "PORTION");
        Assert.True(recipe.IsActive);

        recipe.Deactivate();
        Assert.False(recipe.IsActive);

        recipe.Activate();
        Assert.True(recipe.IsActive);
    }
}
