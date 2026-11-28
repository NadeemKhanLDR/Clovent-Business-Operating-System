using System;
using System.Collections.Generic;
using Clovent.Inventory.Application.Recipes.Dtos;
using Clovent.Inventory.Recipes;
using MediatR;

namespace Clovent.Inventory.Application.Recipes.Commands;

/// <summary>Command to update an existing recipe, its yield, ingredients, modifiers, and versioning.</summary>
public sealed record UpdateRecipeCommand(
    Guid RecipeId,
    string RecipeName,
    decimal YieldQuantity,
    string YieldUnit,
    string? PreparationNotes = null,
    IReadOnlyList<RecipeIngredientInput>? Ingredients = null,
    IReadOnlyList<RecipeModifierInput>? Modifiers = null,
    bool CreateNewVersion = true) : IRequest<RecipeDto>;

/// <summary>Handles <see cref="UpdateRecipeCommand"/>.</summary>
public sealed class UpdateRecipeCommandHandler(IRecipeRepository recipeRepository) : IRequestHandler<UpdateRecipeCommand, RecipeDto>
{
    /// <inheritdoc/>
    public async Task<RecipeDto> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(new RecipeId(request.RecipeId), cancellationToken)
            ?? throw new NotFoundException(nameof(Recipe), request.RecipeId);

        if (request.CreateNewVersion)
        {
            recipe.CreateNewVersionSnapshot();
        }

        recipe.UpdateDetails(request.RecipeName, request.YieldQuantity, request.YieldUnit, request.PreparationNotes);

        if (request.Ingredients != null)
        {
            // Update or replace ingredient set
            var newIngredientIds = request.Ingredients.Select(i => i.IngredientVariantId).ToHashSet();
            var toRemove = recipe.Ingredients.Where(i => !newIngredientIds.Contains(i.IngredientVariantId)).ToList();
            foreach (var rem in toRemove)
            {
                recipe.RemoveIngredient(rem.IngredientVariantId);
            }

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
            var newModNames = request.Modifiers.Select(m => m.ModifierName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var toRemove = recipe.Modifiers.Where(m => !newModNames.Contains(m.ModifierName)).ToList();
            foreach (var rem in toRemove)
            {
                recipe.RemoveModifier(rem.ModifierName);
            }

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

        await recipeRepository.UpdateAsync(recipe, cancellationToken);
        return RecipeDto.FromDomain(recipe);
    }
}

/// <summary>Command to delete a recipe by ID.</summary>
public sealed record DeleteRecipeCommand(Guid RecipeId) : IRequest<bool>;

/// <summary>Handles <see cref="DeleteRecipeCommand"/>.</summary>
public sealed class DeleteRecipeCommandHandler(IRecipeRepository recipeRepository) : IRequestHandler<DeleteRecipeCommand, bool>
{
    /// <inheritdoc/>
    public async Task<bool> Handle(DeleteRecipeCommand request, CancellationToken cancellationToken)
    {
        var id = new RecipeId(request.RecipeId);
        var existing = await recipeRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null) return false;

        await recipeRepository.DeleteAsync(id, cancellationToken);
        return true;
    }
}

/// <summary>Command to activate or deactivate a recipe.</summary>
public sealed record SetRecipeStatusCommand(Guid RecipeId, bool IsActive) : IRequest<RecipeDto>;

/// <summary>Handles <see cref="SetRecipeStatusCommand"/>.</summary>
public sealed class SetRecipeStatusCommandHandler(IRecipeRepository recipeRepository) : IRequestHandler<SetRecipeStatusCommand, RecipeDto>
{
    /// <inheritdoc/>
    public async Task<RecipeDto> Handle(SetRecipeStatusCommand request, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(new RecipeId(request.RecipeId), cancellationToken)
            ?? throw new NotFoundException(nameof(Recipe), request.RecipeId);

        if (request.IsActive) recipe.Activate();
        else recipe.Deactivate();

        await recipeRepository.UpdateAsync(recipe, cancellationToken);
        return RecipeDto.FromDomain(recipe);
    }
}
