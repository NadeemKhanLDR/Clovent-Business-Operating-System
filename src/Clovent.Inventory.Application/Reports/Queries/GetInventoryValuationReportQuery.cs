using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.Inventory.Application.Reports.Dtos;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.Reports.Queries;

/// <summary>Query to retrieve the inventory asset valuation report across warehouses.</summary>
public sealed record GetInventoryValuationReportQuery(Guid? WarehouseId = null) : IRequest<InventoryValuationReportDto>;

/// <summary>Handles <see cref="GetInventoryValuationReportQuery"/>.</summary>
public sealed class GetInventoryValuationReportQueryHandler(
    IWarehouseStockRepository warehouseStockRepository,
    IMediator mediator) : IRequestHandler<GetInventoryValuationReportQuery, InventoryValuationReportDto>
{
    /// <inheritdoc/>
    public async Task<InventoryValuationReportDto> Handle(GetInventoryValuationReportQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<WarehouseStock> stocks = request.WarehouseId.HasValue
            ? await warehouseStockRepository.GetByWarehouseIdAsync(new WarehouseId(request.WarehouseId.Value), cancellationToken)
            : await warehouseStockRepository.GetAllAsync(cancellationToken);

        var items = new List<InventoryValuationItemDto>();

        foreach (var stock in stocks)
        {
            string sku = "";
            string productName = $"Variant {stock.ProductVariantId.Value.ToString()[..8]}";
            string uom = "PCS";
            decimal unitCost = 0m;

            try
            {
                var variant = await mediator.Send(new GetProductVariantByIdQuery(stock.ProductVariantId.Value), cancellationToken);
                if (variant != null)
                {
                    sku = variant.Sku;
                    productName = !string.IsNullOrWhiteSpace(variant.Name) ? variant.Name : variant.Sku;

                    if (variant.UnitOfMeasureId != Guid.Empty)
                    {
                        var uomDto = await mediator.Send(new Clovent.Catalog.Application.UnitsOfMeasure.Queries.GetUnitOfMeasureByIdQuery(variant.UnitOfMeasureId), cancellationToken);
                        if (uomDto != null && !string.IsNullOrWhiteSpace(uomDto.Code))
                        {
                            uom = uomDto.Code;
                        }
                    }
                }

                var prices = await mediator.Send(new ListProductPricesByVariantQuery(stock.ProductVariantId.Value), cancellationToken);
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

            decimal totalValuation = Math.Round(stock.QuantityOnHand * unitCost, 2, MidpointRounding.AwayFromZero);
            bool isLowStock = stock.QuantityOnHand <= stock.MinimumStock;
            bool isNegativeStock = stock.QuantityOnHand < 0m;

            items.Add(new InventoryValuationItemDto(
                stock.Id.Value,
                stock.WarehouseId.Value,
                stock.ProductVariantId.Value,
                sku,
                productName,
                uom,
                stock.QuantityOnHand,
                unitCost,
                totalValuation,
                stock.MinimumStock,
                isLowStock,
                isNegativeStock));
        }

        var totalAssetValuation = Math.Round(items.Sum(i => i.TotalValuation), 2, MidpointRounding.AwayFromZero);
        var lowStockCount = items.Count(i => i.IsLowStock);
        var negativeStockCount = items.Count(i => i.IsNegativeStock);

        return new InventoryValuationReportDto(
            request.WarehouseId,
            DateTimeOffset.UtcNow,
            items.Count,
            lowStockCount,
            negativeStockCount,
            totalAssetValuation,
            items.AsReadOnly());
    }
}
