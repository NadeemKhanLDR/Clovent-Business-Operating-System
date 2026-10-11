using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Commands;

/// <summary>
/// Records stock for a warehouse/variant pairing in one atomic step, whether
/// or not a <see cref="WarehouseStock"/> row already exists for it - the
/// same find-or-create reasoning
/// <c>Clovent.Inventory.Application.Adjustments.Commands.ApplyStockAdjustmentCommand</c>
/// already applies for its own aggregate, used here instead of the two-step
/// Create-then-<see cref="ReceiveStockCommand"/> the dedicated "Receive"
/// action otherwise requires. This is the "Opening
/// Stock" feature: when no row exists yet, the ledger entry is written as
/// <see cref="InventoryTransactionType.OpeningBalance"/> rather than
/// <see cref="InventoryTransactionType.Receipt"/>, marking this quantity as
/// the pairing's very first stock rather than a routine restock.
/// </summary>
public sealed record OpenOrReceiveStockCommand(
    Guid WarehouseId,
    Guid ProductVariantId,
    decimal Quantity,
    string? Notes = null,
    string? ReferenceType = null,
    Guid? ReferenceId = null) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="OpenOrReceiveStockCommand"/>.</summary>
public sealed class OpenOrReceiveStockCommandHandler(
    IWarehouseStockRepository stockRepository,
    IInventoryTransactionRepository transactionRepository,
    IUnitOfWork? unitOfWork = null)
    : IRequestHandler<OpenOrReceiveStockCommand, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(OpenOrReceiveStockCommand request, CancellationToken cancellationToken)
    {
        var warehouseId = new WarehouseId(request.WarehouseId);
        var variantId = new ProductVariantId(request.ProductVariantId);

        if (!string.IsNullOrWhiteSpace(request.ReferenceType) && request.ReferenceId.HasValue && unitOfWork is not null)
        {
            return await unitOfWork.ExecuteStockMovementAtomicallyAsync(
                request.WarehouseId,
                request.ProductVariantId,
                ct => ApplyStockReceiptCoreAsync(warehouseId, variantId, request, ct),
                async ct =>
                {
                    var committedStock = await stockRepository.GetByWarehouseAndVariantAsync(warehouseId, variantId, ct)
                        ?? throw new InvalidOperationException($"WarehouseStock not found after duplicate reference recovery for warehouse '{warehouseId}' and variant '{variantId}'.");
                    return WarehouseStockDto.FromDomain(committedStock);
                },
                cancellationToken);
        }

        return await ApplyStockReceiptCoreAsync(warehouseId, variantId, request, cancellationToken);
    }

    private async Task<WarehouseStockDto> ApplyStockReceiptCoreAsync(
        WarehouseId warehouseId,
        ProductVariantId variantId,
        OpenOrReceiveStockCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ReferenceType) && request.ReferenceId.HasValue)
        {
            var existingTransactions = await transactionRepository.GetByReferenceAsync(
                request.ReferenceType,
                request.ReferenceId.Value,
                cancellationToken);

            var alreadyApplied = existingTransactions.Any(t =>
                t.WarehouseId == warehouseId &&
                t.ProductVariantId == variantId &&
                t.TransactionType is InventoryTransactionType.Receipt or InventoryTransactionType.OpeningBalance);

            if (alreadyApplied)
            {
                var currentStock = await stockRepository.GetByWarehouseAndVariantAsync(warehouseId, variantId, cancellationToken)
                    ?? WarehouseStock.Create(warehouseId, variantId);
                return WarehouseStockDto.FromDomain(currentStock);
            }
        }

        var existingStock = await stockRepository.GetByWarehouseAndVariantAsync(warehouseId, variantId, cancellationToken);
        var isNew = existingStock is null;
        var stock = existingStock ?? WarehouseStock.Create(warehouseId, variantId);
        if (isNew)
        {
            await stockRepository.AddAsync(stock, cancellationToken);
        }

        stock.Receive(request.Quantity);

        var transaction = InventoryTransaction.Create(
            warehouseId,
            variantId,
            isNew ? InventoryTransactionType.OpeningBalance : InventoryTransactionType.Receipt,
            request.Quantity,
            referenceType: request.ReferenceType,
            referenceId: request.ReferenceId,
            notes: request.Notes);
        await transactionRepository.AddAsync(transaction, cancellationToken);

        return WarehouseStockDto.FromDomain(stock);
    }
}
