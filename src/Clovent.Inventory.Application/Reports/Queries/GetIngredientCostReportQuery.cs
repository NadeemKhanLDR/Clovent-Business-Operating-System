using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.Inventory.Application.Reports.Dtos;
using Clovent.Inventory.Recipes;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.Reports.Queries;

/// <summary>Query to generate the raw ingredient cost report.</summary>
public sealed record GetIngredientCostReportQuery(Guid? WarehouseId = null) : IRequest<IngredientCostReportDto>;

/// <summary>Handles <see cref="GetIngredientCostReportQuery"/>.</summary>
public sealed class GetIngredientCostReportQueryHandler(
    IRecipeRepository recipeRepository,
    IWarehouseStockRepository warehouseStockRepository,
    IMediator mediator) : IRequestHandler<GetIngredientCostReportQuery, IngredientCostReportDto>
{
    /// <inheritdoc/>
    public async Task<IngredientCostReportDto> Handle(GetIngredientCostReportQuery request, CancellationToken cancellationToken)
    {
        var allRecipes = await recipeRepository.GetAllAsync(cancellationToken);

        // Map ingredient variant ID to count of recipes used in, and canonical UOM code
        var usageCountMap = new Dictionary<Guid, int>();
        var ingredientUomMap = new Dictionary<Guid, string>();
        var ingredientNameMap = new Dictionary<Guid, string>();

        foreach (var recipe in allRecipes)
        {
            foreach (var ing in recipe.Ingredients)
            {
                usageCountMap[ing.IngredientVariantId] = usageCountMap.GetValueOrDefault(ing.IngredientVariantId, 0) + 1;
                if (!ingredientUomMap.ContainsKey(ing.IngredientVariantId))
                {
                    ingredientUomMap[ing.IngredientVariantId] = ing.UnitOfMeasureCode;
                    ingredientNameMap[ing.IngredientVariantId] = ing.IngredientName;
                }
            }
        }

        // Get stock balances
        IReadOnlyCollection<WarehouseStock> stocks = request.WarehouseId.HasValue
            ? await warehouseStockRepository.GetByWarehouseIdAsync(new WarehouseId(request.WarehouseId.Value), cancellationToken)
            : await warehouseStockRepository.GetAllAsync(cancellationToken);

        var stockByVariant = stocks
            .GroupBy(s => s.ProductVariantId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.QuantityOnHand));

        var items = new List<IngredientCostReportItemDto>();

        foreach (var (variantId, usageCount) in usageCountMap)
        {
            string sku = "";
            string name = ingredientNameMap.GetValueOrDefault(variantId, $"Ingredient {variantId.ToString()[..8]}");
            string uom = ingredientUomMap.GetValueOrDefault(variantId, "PCS");
            decimal unitCost = 0m;

            try
            {
                var variant = await mediator.Send(new GetProductVariantByIdQuery(variantId), cancellationToken);
                if (variant != null)
                {
                    sku = variant.Sku;
                    if (!string.IsNullOrWhiteSpace(variant.Name))
                    {
                        name = variant.Name;
                    }
                }

                var prices = await mediator.Send(new ListProductPricesByVariantQuery(variantId), cancellationToken);
                var activeCost = prices?.FirstOrDefault(p => p.PriceType == nameof(PriceType.Cost) && p.Status == "Active");
                if (activeCost != null)
                {
                    unitCost = activeCost.Amount;
                }
            }
            catch
            {
                // Fallback to defaults
            }

            decimal onHand = stockByVariant.GetValueOrDefault(variantId, 0m);
            decimal valuation = Math.Round(onHand * unitCost, 2, MidpointRounding.AwayFromZero);

            items.Add(new IngredientCostReportItemDto(
                variantId,
                sku,
                name,
                uom,
                unitCost,
                onHand,
                valuation,
                usageCount));
        }

        decimal totalValuation = Math.Round(items.Sum(i => i.TotalStockValuation), 2, MidpointRounding.AwayFromZero);

        return new IngredientCostReportDto(
            DateTimeOffset.UtcNow,
            items.Count,
            totalValuation,
            items.OrderByDescending(i => i.TotalStockValuation).ToList().AsReadOnly());
    }
}
