using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.Inventory.Application.Reports.Dtos;
using Clovent.Inventory.Recipes;
using Clovent.Inventory.Transactions;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.Reports.Queries;

/// <summary>Query to compare theoretical vs actual ingredient consumption and variance.</summary>
public sealed record GetStockVarianceReportQuery(
    Guid WarehouseId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc) : IRequest<StockVarianceReportDto>;

/// <summary>Handles <see cref="GetStockVarianceReportQuery"/>.</summary>
public sealed class GetStockVarianceReportQueryHandler(
    IInventoryTransactionRepository transactionRepository,
    IMediator mediator) : IRequestHandler<GetStockVarianceReportQuery, StockVarianceReportDto>
{
    /// <inheritdoc/>
    public async Task<StockVarianceReportDto> Handle(GetStockVarianceReportQuery request, CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.GetByWarehouseIdAsync(new WarehouseId(request.WarehouseId), cancellationToken);

        var periodTxs = transactions
            .Where(t => t.OccurredAtUtc >= request.FromUtc && t.OccurredAtUtc <= request.ToUtc)
            .ToList();

        // Group transactions by variant
        var variants = periodTxs.Select(t => t.ProductVariantId.Value).Distinct().ToList();

        var items = new List<StockVarianceItemDto>();

        foreach (var variantId in variants)
        {
            var variantTxs = periodTxs.Where(t => t.ProductVariantId.Value == variantId).ToList();

            // Theoretical consumption from sales order postings
            var theoreticalUsage = variantTxs
                .Where(t => t.TransactionType == InventoryTransactionType.Issue && t.ReferenceType == "Order")
                .Sum(t => t.Quantity);

            // Actual consumption includes sales issues plus adjustments/wastage
            var wastageUsage = variantTxs
                .Where(t => t.TransactionType == InventoryTransactionType.Adjustment &&
                           (IsWastage(t.Notes) || IsWastage(t.ReferenceType)))
                .Sum(t => t.Quantity);

            var actualUsage = theoreticalUsage + wastageUsage;
            var varianceQuantity = actualUsage - theoreticalUsage;

            string sku = "";
            string name = $"Variant {variantId.ToString()[..8]}";
            string uom = "PCS";
            decimal unitCost = 0m;

            try
            {
                var variant = await mediator.Send(new GetProductVariantByIdQuery(variantId), cancellationToken);
                if (variant != null)
                {
                    sku = variant.Sku;
                    name = !string.IsNullOrWhiteSpace(variant.Name) ? variant.Name : variant.Sku;

                    if (variant.UnitOfMeasureId != Guid.Empty)
                    {
                        var uomDto = await mediator.Send(new Clovent.Catalog.Application.UnitsOfMeasure.Queries.GetUnitOfMeasureByIdQuery(variant.UnitOfMeasureId), cancellationToken);
                        if (uomDto != null && !string.IsNullOrWhiteSpace(uomDto.Code))
                        {
                            uom = uomDto.Code;
                        }
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

            decimal varianceCost = Math.Round(varianceQuantity * unitCost, 2, MidpointRounding.AwayFromZero);

            string classification = varianceQuantity switch
            {
                0m => "Normal",
                > 0m => "Shrinkage / Waste",
                _ => "Surplus"
            };

            items.Add(new StockVarianceItemDto(
                variantId,
                sku,
                name,
                uom,
                theoreticalUsage,
                actualUsage,
                varianceQuantity,
                unitCost,
                varianceCost,
                classification));
        }

        decimal totalTheoreticalCost = Math.Round(items.Sum(i => i.TheoreticalQuantity * i.UnitCost), 2, MidpointRounding.AwayFromZero);
        decimal totalActualCost = Math.Round(items.Sum(i => i.ActualQuantity * i.UnitCost), 2, MidpointRounding.AwayFromZero);
        decimal totalVarianceCost = Math.Round(items.Sum(i => i.VarianceCost), 2, MidpointRounding.AwayFromZero);

        return new StockVarianceReportDto(
            request.WarehouseId,
            request.FromUtc,
            request.ToUtc,
            items.Count,
            totalTheoreticalCost,
            totalActualCost,
            totalVarianceCost,
            items.AsReadOnly());
    }

    private static bool IsWastage(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.Contains("waste", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("spoil", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("damage", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("expir", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("scrap", StringComparison.OrdinalIgnoreCase);
    }
}
