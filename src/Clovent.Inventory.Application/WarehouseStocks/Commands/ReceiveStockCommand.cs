using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Commands;

/// <summary>
/// Receives stock into a warehouse balance and records the corresponding
/// <see cref="InventoryTransaction"/> ledger entry - both in one handler,
/// since <see cref="WarehouseStock"/> itself has no visibility into the
/// transaction ledger (see its own doc comment).
/// </summary>
public sealed record ReceiveStockCommand(Guid WarehouseStockId, decimal Quantity, string? Notes = null) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="ReceiveStockCommand"/>.</summary>
public sealed class ReceiveStockCommandHandler(IWarehouseStockRepository stockRepository, IInventoryTransactionRepository transactionRepository)
    : IRequestHandler<ReceiveStockCommand, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(ReceiveStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await stockRepository.GetByIdAsync(new WarehouseStockId(request.WarehouseStockId), cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), request.WarehouseStockId);

        stock.Receive(request.Quantity);

        var transaction = InventoryTransaction.Create(
            stock.WarehouseId, stock.ProductVariantId, InventoryTransactionType.Receipt, request.Quantity, notes: request.Notes);
        await transactionRepository.AddAsync(transaction, cancellationToken);

        return WarehouseStockDto.FromDomain(stock);
    }
}
