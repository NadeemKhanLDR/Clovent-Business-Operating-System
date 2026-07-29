using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Application.Adjustments.Dtos;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.Adjustments.Commands;

/// <summary>
/// Applies a pending stock adjustment: transitions it to
/// <see cref="StockAdjustmentStatus.Applied"/>, mutates the corresponding
/// <see cref="WarehouseStock"/> balance (<see cref="WarehouseStock.Receive"/>
/// for <see cref="StockAdjustmentType.Increase"/>,
/// <see cref="WarehouseStock.Issue"/> for <see cref="StockAdjustmentType.Decrease"/>),
/// and records an <see cref="InventoryTransaction"/> referencing the
/// adjustment - all in one handler, since none of these three aggregates
/// has visibility into the others (the established "cross-aggregate
/// consistency is the handler's job" pattern).
/// </summary>
public sealed record ApplyStockAdjustmentCommand(Guid StockAdjustmentId) : IRequest<StockAdjustmentDto>;

/// <summary>Handles <see cref="ApplyStockAdjustmentCommand"/>.</summary>
public sealed class ApplyStockAdjustmentCommandHandler(
    IStockAdjustmentRepository adjustmentRepository,
    IWarehouseStockRepository stockRepository,
    IInventoryTransactionRepository transactionRepository) : IRequestHandler<ApplyStockAdjustmentCommand, StockAdjustmentDto>
{
    /// <inheritdoc/>
    public async Task<StockAdjustmentDto> Handle(ApplyStockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = await adjustmentRepository.GetByIdAsync(new StockAdjustmentId(request.StockAdjustmentId), cancellationToken)
            ?? throw new NotFoundException(nameof(StockAdjustment), request.StockAdjustmentId);

        var existingStock = await stockRepository.GetByWarehouseAndVariantAsync(adjustment.WarehouseId, adjustment.ProductVariantId, cancellationToken);
        var stock = existingStock ?? WarehouseStock.Create(adjustment.WarehouseId, adjustment.ProductVariantId);
        if (existingStock is null)
        {
            await stockRepository.AddAsync(stock, cancellationToken);
        }

        adjustment.Apply();

        if (adjustment.AdjustmentType == StockAdjustmentType.Increase)
        {
            stock.Receive(adjustment.Quantity);
        }
        else
        {
            stock.Issue(adjustment.Quantity);
        }

        var transaction = InventoryTransaction.Create(
            adjustment.WarehouseId,
            adjustment.ProductVariantId,
            InventoryTransactionType.Adjustment,
            adjustment.Quantity,
            referenceType: nameof(StockAdjustment),
            referenceId: adjustment.Id.Value,
            notes: adjustment.Reason);
        await transactionRepository.AddAsync(transaction, cancellationToken);

        return StockAdjustmentDto.FromDomain(adjustment);
    }
}
