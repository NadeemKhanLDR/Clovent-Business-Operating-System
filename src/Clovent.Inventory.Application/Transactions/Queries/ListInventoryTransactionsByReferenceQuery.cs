using Clovent.Inventory.Application.Transactions.Dtos;
using Clovent.Inventory.Transactions;
using MediatR;

namespace Clovent.Inventory.Application.Transactions.Queries;

/// <summary>
/// Retrieves every transaction recorded against one originating document -
/// e.g. <c>("Order", orderId)</c>. The read side of the idempotency check a
/// caller performs before moving stock for a document a second time; see
/// <see cref="IInventoryTransactionRepository.GetByReferenceAsync"/>.
/// </summary>
public sealed record ListInventoryTransactionsByReferenceQuery(string ReferenceType, Guid ReferenceId)
    : IRequest<IReadOnlyCollection<InventoryTransactionDto>>;

/// <summary>Handles <see cref="ListInventoryTransactionsByReferenceQuery"/>.</summary>
public sealed class ListInventoryTransactionsByReferenceQueryHandler(IInventoryTransactionRepository repository)
    : IRequestHandler<ListInventoryTransactionsByReferenceQuery, IReadOnlyCollection<InventoryTransactionDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventoryTransactionDto>> Handle(ListInventoryTransactionsByReferenceQuery request, CancellationToken cancellationToken)
    {
        var transactions = await repository.GetByReferenceAsync(request.ReferenceType, request.ReferenceId, cancellationToken);
        return [.. transactions.Select(InventoryTransactionDto.FromDomain)];
    }
}
