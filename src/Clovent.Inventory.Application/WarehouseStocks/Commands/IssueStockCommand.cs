using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Commands;

/// <summary>
/// Issues stock out of a warehouse balance and records the corresponding
/// <see cref="InventoryTransaction"/> ledger entry.
/// </summary>
/// <remarks>
/// <paramref name="ReferenceType"/>/<paramref name="ReferenceId"/> identify the
/// document this movement belongs to (e.g. <c>("Order", orderId)</c>), stamping
/// the ledger row so a caller can later ask whether that document has already
/// moved stock - the basis of an idempotent retry. Both are optional and
/// default to <see langword="null"/>, so a plain manual issue from the stock
/// screen is unchanged.
/// </remarks>
public sealed record IssueStockCommand(
    Guid WarehouseStockId,
    decimal Quantity,
    string? Notes = null,
    string? ReferenceType = null,
    Guid? ReferenceId = null) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="IssueStockCommand"/>.</summary>
public sealed class IssueStockCommandHandler(
    IWarehouseStockRepository stockRepository,
    IInventoryTransactionRepository transactionRepository,
    IUnitOfWork? unitOfWork = null)
    : IRequestHandler<IssueStockCommand, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(IssueStockCommand request, CancellationToken cancellationToken)
    {
        var stockId = new WarehouseStockId(request.WarehouseStockId);
        var initialStock = await stockRepository.GetByIdAsync(stockId, cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), request.WarehouseStockId);

        if (!string.IsNullOrWhiteSpace(request.ReferenceType) && request.ReferenceId.HasValue && unitOfWork is not null)
        {
            return await unitOfWork.ExecuteStockMovementAtomicallyAsync(
                initialStock.WarehouseId.Value,
                initialStock.ProductVariantId.Value,
                ct => ApplyStockIssueCoreAsync(stockId, request, ct),
                async ct =>
                {
                    var committedStock = await stockRepository.GetByIdAsync(stockId, ct)
                        ?? throw new InvalidOperationException($"WarehouseStock not found after duplicate reference recovery for warehouse stock '{stockId}'.");
                    return WarehouseStockDto.FromDomain(committedStock);
                },
                cancellationToken);
        }

        return await ApplyStockIssueCoreAsync(stockId, request, cancellationToken);
    }

    private async Task<WarehouseStockDto> ApplyStockIssueCoreAsync(
        WarehouseStockId stockId,
        IssueStockCommand request,
        CancellationToken cancellationToken)
    {
        var stock = await stockRepository.GetByIdAsync(stockId, cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), stockId.Value);

        if (!string.IsNullOrWhiteSpace(request.ReferenceType) && request.ReferenceId.HasValue)
        {
            var existingTransactions = await transactionRepository.GetByReferenceAsync(
                request.ReferenceType,
                request.ReferenceId.Value,
                cancellationToken);

            var alreadyApplied = existingTransactions.Any(t =>
                t.WarehouseId == stock.WarehouseId &&
                t.ProductVariantId == stock.ProductVariantId &&
                t.TransactionType == InventoryTransactionType.Issue);

            if (alreadyApplied)
            {
                return WarehouseStockDto.FromDomain(stock);
            }
        }

        stock.Issue(request.Quantity);

        var transaction = InventoryTransaction.Create(
            stock.WarehouseId,
            stock.ProductVariantId,
            InventoryTransactionType.Issue,
            request.Quantity,
            referenceType: request.ReferenceType,
            referenceId: request.ReferenceId,
            notes: request.Notes);
        await transactionRepository.AddAsync(transaction, cancellationToken);

        return WarehouseStockDto.FromDomain(stock);
    }
}
