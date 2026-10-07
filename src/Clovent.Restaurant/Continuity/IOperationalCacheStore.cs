namespace Clovent.Restaurant.Continuity;

/// <summary>
/// Storage contract for local encrypted operational cache snapshots on a POS terminal.
/// </summary>
public interface IOperationalCacheStore
{
    /// <summary>Persists an operational cache snapshot atomically and with cryptographic protection.</summary>
    Task SaveSnapshotAsync(OperationalCacheSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Loads and decrypts the operational cache snapshot for the specified terminal.</summary>
    Task<OperationalCacheSnapshot?> LoadSnapshotAsync(Guid terminalId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves only the metadata header of the cached snapshot without deserializing the full payload.</summary>
    Task<OperationalCacheMetadata?> GetMetadataAsync(Guid terminalId, CancellationToken cancellationToken = default);

    /// <summary>Evaluates whether a valid, non-corrupt cache exists for this terminal and branch.</summary>
    Task<bool> HasValidCacheAsync(Guid branchId, Guid terminalId, CacheFreshnessPolicy policy, CancellationToken cancellationToken = default);
}
