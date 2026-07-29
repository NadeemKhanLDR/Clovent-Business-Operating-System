using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.Transactions.Dtos;
using Clovent.Inventory.Transactions;
using MediatR;

namespace Clovent.Inventory.Application.Transactions.Queries;

/// <summary>Retrieves every transaction recorded for a product variant, across every warehouse - the "Stock History" feature's per-product view.</summary>
public sealed record ListInventoryTransactionsByProductQuery(Guid ProductVariantId) : IRequest<IReadOnlyCollection<InventoryTransactionDto>>;

/// <summary>Handles <see cref="ListInventoryTransactionsByProductQuery"/>.</summary>
public sealed class ListInventoryTransactionsByProductQueryHandler(IInventoryTransactionRepository repository)
    : IRequestHandler<ListInventoryTransactionsByProductQuery, IReadOnlyCollection<InventoryTransactionDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventoryTransactionDto>> Handle(ListInventoryTransactionsByProductQuery request, CancellationToken cancellationToken)
    {
        var transactions = await repository.GetByProductVariantIdAsync(new ProductVariantId(request.ProductVariantId), cancellationToken);
        return [.. transactions.Select(InventoryTransactionDto.FromDomain)];
    }
}
