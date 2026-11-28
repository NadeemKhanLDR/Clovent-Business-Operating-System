using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.Inventory.Application.Reports.Dtos;
using Clovent.Inventory.Transactions;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.Reports.Queries;

/// <summary>Query to generate the kitchen wastage and spoilage report.</summary>
public sealed record GetWastageReportQuery(
    Guid? WarehouseId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null) : IRequest<WastageReportDto>;

/// <summary>Handles <see cref="GetWastageReportQuery"/>.</summary>
public sealed class GetWastageReportQueryHandler(
    IInventoryTransactionRepository transactionRepository,
    IMediator mediator) : IRequestHandler<GetWastageReportQuery, WastageReportDto>
{
    /// <inheritdoc/>
    public async Task<WastageReportDto> Handle(GetWastageReportQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<InventoryTransaction> allTxs;

        if (request.WarehouseId.HasValue)
        {
            allTxs = await transactionRepository.GetByWarehouseIdAsync(new WarehouseId(request.WarehouseId.Value), cancellationToken);
        }
        else
        {
            allTxs = await transactionRepository.GetRecentAsync(1000, cancellationToken);
        }

        var wastageTxs = allTxs
            .Where(t => t.TransactionType == InventoryTransactionType.Adjustment &&
                       (IsWastageNote(t.Notes) || IsWastageNote(t.ReferenceType)))
            .Where(t => !request.FromUtc.HasValue || t.OccurredAtUtc >= request.FromUtc.Value)
            .Where(t => !request.ToUtc.HasValue || t.OccurredAtUtc <= request.ToUtc.Value)
            .OrderByDescending(t => t.OccurredAtUtc)
            .ToList();

        var items = new List<WastageReportItemDto>();

        foreach (var tx in wastageTxs)
        {
            string sku = "";
            string name = $"Variant {tx.ProductVariantId.Value.ToString()[..8]}";
            string uom = "PCS";
            decimal unitCost = 0m;

            try
            {
                var variant = await mediator.Send(new GetProductVariantByIdQuery(tx.ProductVariantId.Value), cancellationToken);
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

                var prices = await mediator.Send(new ListProductPricesByVariantQuery(tx.ProductVariantId.Value), cancellationToken);
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

            var wastageQty = Math.Abs(tx.Quantity);
            var estimatedCost = Math.Round(wastageQty * unitCost, 2, MidpointRounding.AwayFromZero);
            var reason = !string.IsNullOrWhiteSpace(tx.Notes) ? tx.Notes : (tx.ReferenceType ?? "Wastage");

            items.Add(new WastageReportItemDto(
                tx.Id.Value,
                tx.WarehouseId.Value,
                tx.ProductVariantId.Value,
                sku,
                name,
                wastageQty,
                uom,
                unitCost,
                estimatedCost,
                reason,
                tx.OccurredAtUtc));
        }

        var totalQuantity = items.Sum(i => i.Quantity);
        var totalCost = Math.Round(items.Sum(i => i.EstimatedWastageCost), 2, MidpointRounding.AwayFromZero);

        return new WastageReportDto(
            request.WarehouseId,
            request.FromUtc ?? DateTimeOffset.MinValue,
            request.ToUtc ?? DateTimeOffset.UtcNow,
            items.Count,
            totalQuantity,
            totalCost,
            items.AsReadOnly());
    }

    private static bool IsWastageNote(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return false;
        return notes.Contains("waste", StringComparison.OrdinalIgnoreCase) ||
               notes.Contains("spoil", StringComparison.OrdinalIgnoreCase) ||
               notes.Contains("damage", StringComparison.OrdinalIgnoreCase) ||
               notes.Contains("expir", StringComparison.OrdinalIgnoreCase) ||
               notes.Contains("scrap", StringComparison.OrdinalIgnoreCase);
    }
}
