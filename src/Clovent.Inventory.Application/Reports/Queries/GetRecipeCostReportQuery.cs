using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Prices;
using Clovent.Inventory.Application.Recipes.Dtos;
using Clovent.Inventory.Application.Recipes.Queries;
using Clovent.Inventory.Application.Reports.Dtos;
using Clovent.Inventory.Costing;
using Clovent.Inventory.Recipes;
using MediatR;

namespace Clovent.Inventory.Application.Reports.Queries;

/// <summary>Query to retrieve the menu item and recipe food cost summary report.</summary>
public sealed record GetRecipeCostReportQuery(decimal? FoodCostAlertThreshold = 35.0m) : IRequest<RecipeCostReportDto>;

/// <summary>Handles <see cref="GetRecipeCostReportQuery"/>.</summary>
public sealed class GetRecipeCostReportQueryHandler(
    IRecipeRepository recipeRepository,
    IMediator mediator) : IRequestHandler<GetRecipeCostReportQuery, RecipeCostReportDto>
{
    private const decimal DefaultAlertThreshold = 35.0m;

    /// <inheritdoc/>
    public async Task<RecipeCostReportDto> Handle(GetRecipeCostReportQuery request, CancellationToken cancellationToken)
    {
        var threshold = request.FoodCostAlertThreshold ?? DefaultAlertThreshold;
        var recipes = await recipeRepository.GetAllAsync(cancellationToken);
        var activeRecipes = recipes.Where(r => r.IsActive).ToList();

        var reportItems = new List<RecipeCostReportItemDto>();

        foreach (var recipe in activeRecipes)
        {
            var costResult = await mediator.Send(new CalculateRecipeCostQuery(recipe.Id.Value), cancellationToken);
            if (costResult is null) continue;

            bool isAlert = costResult.FoodCostPercentage > threshold;

            reportItems.Add(new RecipeCostReportItemDto(
                recipe.ProductVariantId,
                recipe.RecipeName,
                costResult.YieldQuantity,
                costResult.YieldUnit,
                costResult.CostPerPortion,
                costResult.SellingPrice,
                costResult.FoodCostPercentage,
                costResult.GrossProfitPerPortion,
                costResult.GrossMarginPercentage,
                costResult.Ingredients.Count,
                isAlert));
        }

        var avgFoodCost = reportItems.Count > 0
            ? Math.Round(reportItems.Average(i => i.FoodCostPercentage), 2, MidpointRounding.AwayFromZero)
            : 0m;

        return new RecipeCostReportDto(
            DateTimeOffset.UtcNow,
            reportItems.Count,
            avgFoodCost,
            reportItems.AsReadOnly());
    }
}
