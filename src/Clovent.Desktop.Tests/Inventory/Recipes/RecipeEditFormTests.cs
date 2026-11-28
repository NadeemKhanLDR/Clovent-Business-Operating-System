using System;
using System.Collections.Generic;
using Clovent.Desktop.Inventory.Recipes;
using Clovent.Inventory.Application.Recipes.Dtos;
using Xunit;

namespace Clovent.Desktop.Tests.Inventory.Recipes;

public class RecipeEditFormTests
{
    [Fact]
    public void IngredientEditItem_Calculates_GrossQuantity_WithWastagePercentage()
    {
        // 500g net with 10% trim wastage -> 500 * (1 + 0.10) = 550g gross
        var item = new RecipeEditForm.IngredientEditItem
        {
            Quantity = 0.5000m,
            UnitOfMeasureCode = "KG",
            WastagePercentage = 10m
        };

        Assert.Equal(0.5500m, item.GrossQuantityRequired);
    }

    [Fact]
    public void IngredientEditItem_GrossQuantity_ZeroWastage_EqualsNetQuantity()
    {
        var item = new RecipeEditForm.IngredientEditItem
        {
            Quantity = 2.0000m,
            UnitOfMeasureCode = "L",
            WastagePercentage = 0m
        };

        Assert.Equal(2.0000m, item.GrossQuantityRequired);
    }

    [Fact]
    public void RecipeEditForm_InitializesDefaults_ForNewRecipe()
    {
        var sellable = new List<(Guid Id, string Display)>
        {
            (Guid.NewGuid(), "Pepperoni Pizza 12\"")
        };
        var ingredients = new List<(Guid Id, string Display, string Sku)>
        {
            (Guid.NewGuid(), "Pizza Dough", "ING-001"),
            (Guid.NewGuid(), "Tomato Sauce", "ING-002"),
            (Guid.NewGuid(), "Mozzarella", "ING-003")
        };

        using var form = new RecipeEditForm(sellable, ingredients);

        Assert.Equal(1m, form.YieldQuantity);
        Assert.Equal("Portion", form.YieldUnit);
        Assert.True(form.IsRecipeActive);
        Assert.False(form.CreateNewVersion);
    }

    [Fact]
    public void RecipeEditForm_PopulatesExistingRecipeData()
    {
        var variantId = Guid.NewGuid();
        var ingVariantId = Guid.NewGuid();

        var existingRecipe = new RecipeDto(
            Id: Guid.NewGuid(),
            ProductVariantId: variantId,
            RecipeName: "Signature Pasta",
            YieldQuantity: 2m,
            YieldUnit: "Plates",
            Version: 3,
            IsActive: true,
            PreparationNotes: "Cook al dente for 8 mins",
            Ingredients:
            [
                new RecipeIngredientDto(ingVariantId, "Semolina Pasta", 0.25m, "KG", 5m, 3.50m, 0.2625m)
            ],
            Modifiers: [],
            Versions:
            [
                new RecipeVersionDto(1, DateTimeOffset.UtcNow.AddMonths(-2), 2m, "Plates", []),
                new RecipeVersionDto(2, DateTimeOffset.UtcNow.AddMonths(-1), 2m, "Plates", [])
            ],
            CreatedAtUtc: DateTimeOffset.UtcNow.AddMonths(-2),
            UpdatedAtUtc: DateTimeOffset.UtcNow);

        var sellable = new List<(Guid Id, string Display)>
        {
            (variantId, "Signature Pasta")
        };
        var ingredients = new List<(Guid Id, string Display, string Sku)>
        {
            (ingVariantId, "Semolina Pasta", "ING-PASTA")
        };

        using var form = new RecipeEditForm(sellable, ingredients, existingRecipe);

        Assert.Equal("Signature Pasta", form.RecipeName);
        Assert.Equal(2m, form.YieldQuantity);
        Assert.Equal("Plates", form.YieldUnit);
        Assert.Equal("Cook al dente for 8 mins", form.PreparationNotes);
        Assert.True(form.IsRecipeActive);
    }
}
