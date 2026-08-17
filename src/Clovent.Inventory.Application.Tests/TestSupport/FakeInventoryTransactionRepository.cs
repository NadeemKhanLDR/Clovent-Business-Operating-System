using Clovent.Catalog.Variants;
using Clovent.Inventory.Transactions;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.Application.Tests.TestSupport;

internal sealed class FakeInventoryTransactionRepository : IInventoryTransactionRepository
{
    private readonly Dictionary<InventoryTransactionId, InventoryTransaction> _transactions = [];

    public void Add(InventoryTransaction transaction) => _transactions[transaction.Id] = transaction;

    public Task<InventoryTransaction?> GetByIdAsync(InventoryTransactionId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_transactions.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<InventoryTransaction>> GetByWarehouseIdAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<InventoryTransaction>>([.. _transactions.Values.Where(t => t.WarehouseId == warehouseId)]);

    public Task<IReadOnlyCollection<InventoryTransaction>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<InventoryTransaction>>([.. _transactions.Values.Where(t => t.ProductVariantId == productVariantId)]);

    public Task<IReadOnlyCollection<InventoryTransaction>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<InventoryTransaction>>(
            [.. _transactions.Values.OrderByDescending(t => t.OccurredAtUtc).Take(count)]);

    public Task<IReadOnlyCollection<InventoryTransaction>> GetByReferenceAsync(string referenceType, Guid referenceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<InventoryTransaction>>(
            [.. _transactions.Values.Where(t => t.ReferenceType == referenceType && t.ReferenceId == referenceId)]);

    public Task AddAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        _transactions[transaction.Id] = transaction;
        return Task.CompletedTask;
    }
}
