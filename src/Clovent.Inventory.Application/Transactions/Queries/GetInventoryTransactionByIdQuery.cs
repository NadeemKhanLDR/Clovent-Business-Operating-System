using Clovent.Inventory.Application.Transactions.Dtos;
using Clovent.Inventory.Transactions;
using MediatR;

namespace Clovent.Inventory.Application.Transactions.Queries;

/// <summary>Retrieves a single inventory transaction by identity.</summary>
public sealed record GetInventoryTransactionByIdQuery(Guid InventoryTransactionId) : IRequest<InventoryTransactionDto>;

/// <summary>Handles <see cref="GetInventoryTransactionByIdQuery"/>.</summary>
public sealed class GetInventoryTransactionByIdQueryHandler(IInventoryTransactionRepository repository)
    : IRequestHandler<GetInventoryTransactionByIdQuery, InventoryTransactionDto>
{
    /// <inheritdoc/>
    public async Task<InventoryTransactionDto> Handle(GetInventoryTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await repository.GetByIdAsync(new InventoryTransactionId(request.InventoryTransactionId), cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryTransaction), request.InventoryTransactionId);

        return InventoryTransactionDto.FromDomain(transaction);
    }
}
