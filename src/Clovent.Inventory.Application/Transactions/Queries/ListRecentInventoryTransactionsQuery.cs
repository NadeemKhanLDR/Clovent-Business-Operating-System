using Clovent.Inventory.Application.Transactions.Dtos;
using Clovent.Inventory.Transactions;
using MediatR;

namespace Clovent.Inventory.Application.Transactions.Queries;

/// <summary>Retrieves the most recent transactions across every warehouse, newest first - backs the Dashboard's "Recent Stock Movements" widget and the Inventory Transactions screen.</summary>
public sealed record ListRecentInventoryTransactionsQuery(int Count = 20) : IRequest<IReadOnlyCollection<InventoryTransactionDto>>;

/// <summary>Handles <see cref="ListRecentInventoryTransactionsQuery"/>.</summary>
public sealed class ListRecentInventoryTransactionsQueryHandler(IInventoryTransactionRepository repository)
    : IRequestHandler<ListRecentInventoryTransactionsQuery, IReadOnlyCollection<InventoryTransactionDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventoryTransactionDto>> Handle(ListRecentInventoryTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await repository.GetRecentAsync(request.Count, cancellationToken);
        return [.. transactions.Select(InventoryTransactionDto.FromDomain)];
    }
}
