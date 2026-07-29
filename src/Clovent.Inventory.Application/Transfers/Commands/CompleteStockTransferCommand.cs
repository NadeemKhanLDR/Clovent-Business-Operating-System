using Clovent.Inventory.Application.Transfers.Dtos;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.Transfers;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.Transfers.Commands;

/// <summary>
/// Completes a pending stock transfer: transitions it to
/// <see cref="StockTransferStatus.Completed"/>, issues the quantity from
/// the source <see cref="WarehouseStock"/> (which must already exist),
/// receives it into the destination <see cref="WarehouseStock"/> (created
/// on demand if this is the first time that warehouse holds the variant),
/// and records two <see cref="InventoryTransaction"/> entries (TransferOut
/// at the source, TransferIn at the destination) - all in one handler, the
/// established "cross-aggregate consistency is the handler's job" pattern.
/// </summary>
public sealed record CompleteStockTransferCommand(Guid StockTransferId) : IRequest<StockTransferDto>;

/// <summary>Handles <see cref="CompleteStockTransferCommand"/>.</summary>
public sealed class CompleteStockTransferCommandHandler(
    IStockTransferRepository transferRepository,
    IWarehouseStockRepository stockRepository,
    IInventoryTransactionRepository transactionRepository) : IRequestHandler<CompleteStockTransferCommand, StockTransferDto>
{
    /// <inheritdoc/>
    public async Task<StockTransferDto> Handle(CompleteStockTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await transferRepository.GetByIdAsync(new StockTransferId(request.StockTransferId), cancellationToken)
            ?? throw new NotFoundException(nameof(StockTransfer), request.StockTransferId);

        var sourceStock = await stockRepository.GetByWarehouseAndVariantAsync(transfer.SourceWarehouseId, transfer.ProductVariantId, cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), $"{transfer.SourceWarehouseId}/{transfer.ProductVariantId}");

        var destinationStock = await stockRepository.GetByWarehouseAndVariantAsync(transfer.DestinationWarehouseId, transfer.ProductVariantId, cancellationToken);
        var isNewDestinationStock = destinationStock is null;
        destinationStock ??= WarehouseStock.Create(transfer.DestinationWarehouseId, transfer.ProductVariantId);
        if (isNewDestinationStock)
        {
            await stockRepository.AddAsync(destinationStock, cancellationToken);
        }

        transfer.Complete();
        sourceStock.Issue(transfer.Quantity);
        destinationStock.Receive(transfer.Quantity);

        await transactionRepository.AddAsync(InventoryTransaction.Create(
            transfer.SourceWarehouseId, transfer.ProductVariantId, InventoryTransactionType.TransferOut, transfer.Quantity,
            referenceType: nameof(StockTransfer), referenceId: transfer.Id.Value), cancellationToken);

        await transactionRepository.AddAsync(InventoryTransaction.Create(
            transfer.DestinationWarehouseId, transfer.ProductVariantId, InventoryTransactionType.TransferIn, transfer.Quantity,
            referenceType: nameof(StockTransfer), referenceId: transfer.Id.Value), cancellationToken);

        return StockTransferDto.FromDomain(transfer);
    }
}
