using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.Inventory.Application.Recipes.Dtos;
using Clovent.Inventory.Costing;
using Clovent.Inventory.Recipes;
using MediatR;

namespace Clovent.Inventory.Application.Recipes.Queries;

/// <summary>Query to retrieve a recipe by its ID.</summary>
public sealed record GetRecipeByIdQuery(Guid RecipeId) : IRequest<RecipeDto?>;

/// <summary>Handles <see cref="GetRecipeByIdQuery"/>.</summary>
public sealed class GetRecipeByIdQueryHandler(IRecipeRepository recipeRepository) : IRequestHandler<GetRecipeByIdQuery, RecipeDto?>
{
    /// <inheritdoc/>
    public async Task<RecipeDto?> Handle(GetRecipeByIdQuery request, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(new RecipeId(request.RecipeId), cancellationToken);
        return recipe == null ? null : RecipeDto.FromDomain(recipe);
    }
}

/// <summary>Query to retrieve the active recipe for a product variant.</summary>
public sealed record GetRecipeByProductVariantIdQuery(Guid ProductVariantId) : IRequest<RecipeDto?>;

/// <summary>Handles <see cref="GetRecipeByProductVariantIdQuery"/>.</summary>
public sealed class GetRecipeByProductVariantIdQueryHandler(IRecipeRepository recipeRepository) : IRequestHandler<GetRecipeByProductVariantIdQuery, RecipeDto?>
{
    /// <inheritdoc/>
    public async Task<RecipeDto?> Handle(GetRecipeByProductVariantIdQuery request, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByProductVariantIdAsync(request.ProductVariantId, cancellationToken);
        return recipe == null ? null : RecipeDto.FromDomain(recipe);
    }
}

/// <summary>Query to list all recipes.</summary>
public sealed record ListRecipesQuery : IRequest<IReadOnlyList<RecipeDto>>;

/// <summary>Handles <see cref="ListRecipesQuery"/>.</summary>
public sealed class ListRecipesQueryHandler(IRecipeRepository recipeRepository) : IRequestHandler<ListRecipesQuery, IReadOnlyList<RecipeDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecipeDto>> Handle(ListRecipesQuery request, CancellationToken cancellationToken)
    {
        var recipes = await recipeRepository.GetAllAsync(cancellationToken);
        return recipes.Select(RecipeDto.FromDomain).ToList().AsReadOnly();
    }
}

/// <summary>Query to calculate theoretical food cost and food cost percentage for a recipe.</summary>
public sealed record CalculateRecipeCostQuery(Guid RecipeId, decimal? SellingPriceOverride = null) : IRequest<RecipeCostDto?>;

/// <summary>Handles <see cref="CalculateRecipeCostQuery"/>.</summary>
public sealed class CalculateRecipeCostQueryHandler(
    IRecipeRepository recipeRepository,
    IMediator mediator) : IRequestHandler<CalculateRecipeCostQuery, RecipeCostDto?>
{
    /// <inheritdoc/>
    public async Task<RecipeCostDto?> Handle(CalculateRecipeCostQuery request, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(new RecipeId(request.RecipeId), cancellationToken);
        if (recipe == null) return null;

        // Resolve selling price
        decimal sellingPrice = request.SellingPriceOverride ?? 0m;
        if (!request.SellingPriceOverride.HasValue)
        {
            try
            {
                var prices = await mediator.Send(new ListProductPricesByVariantQuery(recipe.ProductVariantId), cancellationToken);
                var activeSellingPrice = prices?.FirstOrDefault(p => p.PriceType == nameof(PriceType.Selling) && p.Status == "Active");
                if (activeSellingPrice != null)
                {
                    sellingPrice = activeSellingPrice.Amount;
                }
            }
            catch
            {
                // Catalog prices not reachable; keep 0
            }
        }

        // Resolve cost prices for each ingredient variant
        var costMap = new Dictionary<Guid, (decimal UnitCost, string UnitOfMeasureCode)>();
        foreach (var ing in recipe.Ingredients)
        {
            try
            {
                var prices = await mediator.Send(new ListProductPricesByVariantQuery(ing.IngredientVariantId), cancellationToken);
                var activeCost = prices?.FirstOrDefault(p => p.PriceType == nameof(PriceType.Cost) && p.Status == "Active");
                decimal unitCost = ing.CostPerUnitOverride ?? activeCost?.Amount ?? 0m;

                string uom = ing.UnitOfMeasureCode;
                var variant = await mediator.Send(new GetProductVariantByIdQuery(ing.IngredientVariantId), cancellationToken);
                if (variant != null && variant.UnitOfMeasureId != Guid.Empty)
                {
                    try
                    {
                        var uomDto = await mediator.Send(new Clovent.Catalog.Application.UnitsOfMeasure.Queries.GetUnitOfMeasureByIdQuery(variant.UnitOfMeasureId), cancellationToken);
                        if (uomDto != null && !string.IsNullOrWhiteSpace(uomDto.Code))
                        {
                            uom = uomDto.Code;
                        }
                    }
                    catch
                    {
                        // Fallback to recipe line UOM
                    }
                }

                costMap[ing.IngredientVariantId] = (unitCost, uom);
            }
            catch
            {
                costMap[ing.IngredientVariantId] = (ing.CostPerUnitOverride ?? 0m, ing.UnitOfMeasureCode);
            }
        }

        var result = RecipeCostCalculator.CalculateCost(
            recipe,
            vid => costMap.TryGetValue(vid, out var tuple) ? tuple : (0m, ""),
            sellingPrice);

        return RecipeCostDto.FromDomain(result);
    }
}
