using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Commands;

/// <summary>Issues stock out of a warehouse balance and records the corresponding <see cref="InventoryTransaction"/> ledger entry.</summary>
public sealed record IssueStockCommand(Guid WarehouseStockId, decimal Quantity, string? Notes = null) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="IssueStockCommand"/>.</summary>
public sealed class IssueStockCommandHandler(IWarehouseStockRepository stockRepository, IInventoryTransactionRepository transactionRepository)
    : IRequestHandler<IssueStockCommand, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(IssueStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await stockRepository.GetByIdAsync(new WarehouseStockId(request.WarehouseStockId), cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), request.WarehouseStockId);

        stock.Issue(request.Quantity);

        var transaction = InventoryTransaction.Create(
            stock.WarehouseId, stock.ProductVariantId, InventoryTransactionType.Issue, request.Quantity, notes: request.Notes);
        await transactionRepository.AddAsync(transaction, cancellationToken);

        return WarehouseStockDto.FromDomain(stock);
    }
}
