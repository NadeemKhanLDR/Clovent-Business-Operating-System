using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Inventory.Application.Recipes.Commands;
using Clovent.Inventory.Application.Recipes.Dtos;
using Clovent.Inventory.Application.Recipes.Queries;
using Clovent.Inventory.Application.Tests.TestSupport;
using Clovent.Inventory.Recipes;
using Xunit;

namespace Clovent.Inventory.Application.Tests.Recipes;

public class RecipeHandlerTests
{
    private readonly FakeRecipeRepository _recipeRepository = new();

    [Fact]
    public async Task CreateRecipeCommand_CreatesAndPersistsRecipe()
    {
        var handler = new CreateRecipeCommandHandler(_recipeRepository);
        var productVariantId = Guid.NewGuid();
        var beefId = Guid.NewGuid();

        var ingredients = new List<RecipeIngredientInput>
        {
            new(beefId, "Beef Patty", 0.1500m, "KG", 5m, 1200m)
        };

        var modifiers = new List<RecipeModifierInput>
        {
            new("Extra Cheese", Guid.NewGuid(), "Cheese Slice", 1m, "SLICE", 0m, 50m)
        };

        var command = new CreateRecipeCommand(
            productVariantId,
            "Smash Burger",
            1m,
            "PORTION",
            "Double smashed patty burger",
            ingredients,
            modifiers);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Smash Burger", result.RecipeName);
        Assert.Equal(productVariantId, result.ProductVariantId);
        Assert.Single(result.Ingredients);
        Assert.Single(result.Modifiers);
        Assert.True(result.IsActive);

        var persisted = await _recipeRepository.GetByIdAsync(new RecipeId(result.Id));
        Assert.NotNull(persisted);
        Assert.Equal("Smash Burger", persisted.RecipeName);
    }

    [Fact]
    public async Task UpdateRecipeCommand_BumpsVersionWhenRequested()
    {
        var recipe = Recipe.Create(RecipeId.New(), Guid.NewGuid(), "Classic Pizza", 1m, "PIE");
        recipe.AddOrUpdateIngredient(Guid.NewGuid(), "Mozzarella", 0.2000m, "KG", 0m);
        await _recipeRepository.AddAsync(recipe);

        var handler = new UpdateRecipeCommandHandler(_recipeRepository);
        var flourId = Guid.NewGuid();

        var updateCommand = new UpdateRecipeCommand(
            recipe.Id.Value,
            "Classic Pizza Rev 2",
            1m,
            "PIE",
            "Updated dough formulation",
            [new RecipeIngredientInput(flourId, "Italian Flour", 0.2500m, "KG", 0m, 150m)],
            [],
            CreateNewVersion: true);

        var updated = await handler.Handle(updateCommand, CancellationToken.None);

        Assert.Equal(2, updated.Version);
        Assert.Equal("Classic Pizza Rev 2", updated.RecipeName);
        Assert.Single(updated.Ingredients);
        Assert.Equal(flourId, updated.Ingredients[0].IngredientVariantId);
    }

    [Fact]
    public async Task GetRecipeQueries_ReturnExpectedData()
    {
        var variantId = Guid.NewGuid();
        var recipe = Recipe.Create(RecipeId.New(), variantId, "Club Sandwich", 1m, "PORTION");
        await _recipeRepository.AddAsync(recipe);

        var getByIdHandler = new GetRecipeByIdQueryHandler(_recipeRepository);
        var byIdResult = await getByIdHandler.Handle(new GetRecipeByIdQuery(recipe.Id.Value), CancellationToken.None);
        Assert.NotNull(byIdResult);
        Assert.Equal("Club Sandwich", byIdResult.RecipeName);

        var getByVariantHandler = new GetRecipeByProductVariantIdQueryHandler(_recipeRepository);
        var byVariantResult = await getByVariantHandler.Handle(new GetRecipeByProductVariantIdQuery(variantId), CancellationToken.None);
        Assert.NotNull(byVariantResult);
        Assert.Equal(recipe.Id.Value, byVariantResult.Id);

        var listHandler = new ListRecipesQueryHandler(_recipeRepository);
        var listResult = await listHandler.Handle(new ListRecipesQuery(), CancellationToken.None);
        Assert.Single(listResult);
    }
}
