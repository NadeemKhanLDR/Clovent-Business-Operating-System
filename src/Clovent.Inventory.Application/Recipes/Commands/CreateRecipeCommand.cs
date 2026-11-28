using System;
using System.Collections.Generic;
using Clovent.Inventory.Application.Recipes.Dtos;
using Clovent.Inventory.Recipes;
using MediatR;

namespace Clovent.Inventory.Application.Recipes.Commands;

/// <summary>Request ingredient parameter for creating/updating a recipe.</summary>
public sealed record RecipeIngredientInput(
    Guid IngredientVariantId,
    string IngredientName,
    decimal Quantity,
    string UnitOfMeasureCode,
    decimal WastagePercentage = 0m,
    decimal? CostPerUnitOverride = null);

/// <summary>Request modifier parameter for creating/updating a recipe.</summary>
public sealed record RecipeModifierInput(
    string ModifierName,
    Guid IngredientVariantId,
    string IngredientName,
    decimal Quantity,
    string UnitOfMeasureCode,
    decimal WastagePercentage = 0m,
    decimal? CostPerUnitOverride = null);

/// <summary>Command to create a new recipe for a prepared product variant.</summary>
public sealed record CreateRecipeCommand(
    Guid ProductVariantId,
    string RecipeName,
    decimal YieldQuantity = 1.0m,
    string YieldUnit = "Portion",
    string? PreparationNotes = null,
    IReadOnlyList<RecipeIngredientInput>? Ingredients = null,
    IReadOnlyList<RecipeModifierInput>? Modifiers = null) : IRequest<RecipeDto>;

/// <summary>Handles <see cref="CreateRecipeCommand"/>.</summary>
public sealed class CreateRecipeCommandHandler(IRecipeRepository recipeRepository) : IRequestHandler<CreateRecipeCommand, RecipeDto>
{
    /// <inheritdoc/>
    public async Task<RecipeDto> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
    {
        var existing = await recipeRepository.GetByProductVariantIdAsync(request.ProductVariantId, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"An active recipe already exists for product variant '{request.ProductVariantId}'.");
        }

        var recipe = Recipe.Create(
            request.ProductVariantId,
            request.RecipeName,
            request.YieldQuantity,
            request.YieldUnit,
            request.PreparationNotes);

        if (request.Ingredients != null)
        {
            foreach (var ing in request.Ingredients)
            {
                recipe.SetIngredient(new RecipeIngredient(
                    ing.IngredientVariantId,
                    ing.IngredientName,
                    ing.Quantity,
                    ing.UnitOfMeasureCode,
                    ing.WastagePercentage,
                    ing.CostPerUnitOverride));
            }
        }

        if (request.Modifiers != null)
        {
            foreach (var mod in request.Modifiers)
            {
                recipe.SetModifier(new RecipeModifier(
                    mod.ModifierName,
                    mod.IngredientVariantId,
                    mod.IngredientName,
                    mod.Quantity,
                    mod.UnitOfMeasureCode,
                    mod.WastagePercentage,
                    mod.CostPerUnitOverride));
            }
        }

        await recipeRepository.AddAsync(recipe, cancellationToken);
        return RecipeDto.FromDomain(recipe);
    }
}
