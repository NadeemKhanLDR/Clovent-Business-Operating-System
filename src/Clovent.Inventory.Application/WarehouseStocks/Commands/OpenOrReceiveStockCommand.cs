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
public sealed record OpenOrReceiveStockCommand(Guid WarehouseId, Guid ProductVariantId, decimal Quantity, string? Notes = null) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="OpenOrReceiveStockCommand"/>.</summary>
public sealed class OpenOrReceiveStockCommandHandler(IWarehouseStockRepository stockRepository, IInventoryTransactionRepository transactionRepository)
    : IRequestHandler<OpenOrReceiveStockCommand, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(OpenOrReceiveStockCommand request, CancellationToken cancellationToken)
    {
        var warehouseId = new WarehouseId(request.WarehouseId);
        var variantId = new ProductVariantId(request.ProductVariantId);

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
            notes: request.Notes);
        await transactionRepository.AddAsync(transaction, cancellationToken);

        return WarehouseStockDto.FromDomain(stock);
    }
}
