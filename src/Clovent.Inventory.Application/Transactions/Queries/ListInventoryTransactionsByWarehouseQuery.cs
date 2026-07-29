using Clovent.Inventory.Application.Transactions.Dtos;
using Clovent.Inventory.Transactions;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.Transactions.Queries;

/// <summary>Retrieves every transaction recorded at a warehouse.</summary>
public sealed record ListInventoryTransactionsByWarehouseQuery(Guid WarehouseId) : IRequest<IReadOnlyCollection<InventoryTransactionDto>>;

/// <summary>Handles <see cref="ListInventoryTransactionsByWarehouseQuery"/>.</summary>
public sealed class ListInventoryTransactionsByWarehouseQueryHandler(IInventoryTransactionRepository repository)
    : IRequestHandler<ListInventoryTransactionsByWarehouseQuery, IReadOnlyCollection<InventoryTransactionDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventoryTransactionDto>> Handle(ListInventoryTransactionsByWarehouseQuery request, CancellationToken cancellationToken)
    {
        var transactions = await repository.GetByWarehouseIdAsync(new WarehouseId(request.WarehouseId), cancellationToken);
        return [.. transactions.Select(InventoryTransactionDto.FromDomain)];
    }
}
