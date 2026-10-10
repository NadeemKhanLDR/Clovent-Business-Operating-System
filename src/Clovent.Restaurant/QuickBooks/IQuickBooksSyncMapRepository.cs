namespace Clovent.Restaurant.QuickBooks;

/// <summary>
/// Repository interface for persisting and querying bidirectional QuickBooks sync mappings.
/// </summary>
public interface IQuickBooksSyncMapRepository
{
    /// <summary>Finds a mapping record by its CBOS entity identifier and entity type.</summary>
    Task<QuickBooksSyncMap?> GetByLocalEntityAsync(Guid localEntityId, string entityType, CancellationToken cancellationToken = default);

    /// <summary>Finds a mapping record by remote QuickBooks transaction ID (TxnID).</summary>
    Task<QuickBooksSyncMap?> GetByQuickBooksTxnIdAsync(string qbTxnId, CancellationToken cancellationToken = default);

    /// <summary>Finds a mapping record by primary ledger key.</summary>
    Task<QuickBooksSyncMap?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves recent sync mappings with optional status filtering.</summary>
    Task<IReadOnlyList<QuickBooksSyncMap>> GetRecentAsync(
        int limit = 100,
        QuickBooksSyncStatus? statusFilter = null,
        string? entityTypeFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a new sync mapping entry atomically.</summary>
    Task AddAsync(QuickBooksSyncMap map, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing mapping entry.</summary>
    Task UpdateAsync(QuickBooksSyncMap map, CancellationToken cancellationToken = default);
}
