using Clovent.Restaurant.Continuity;

namespace Clovent.Restaurant.Application.Continuity;

/// <summary>
/// Service responsible for building, synchronizing, and validating the local operational cache.
/// </summary>
public interface IOperationalCacheSynchronizer
{
    /// <summary>
    /// Synchronizes the local operational cache by querying authoritative catalog, menu, pricing,
    /// and terminal rules from the online database, and atomically updating the protected local cache.
    /// </summary>
    Task<OperationalCacheSnapshot> SynchronizeAsync(
        Guid companyId,
        string companyName,
        Guid branchId,
        string branchName,
        Guid terminalId,
        string terminalName,
        string terminalCode,
        Guid warehouseId,
        string warehouseName,
        string currencyCode,
        string currencySymbol,
        int currencyDecimals,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves the metadata header of the cached snapshot without loading the entire payload.</summary>
    Task<OperationalCacheMetadata?> GetCurrentMetadataAsync(Guid terminalId, CancellationToken cancellationToken = default);

    /// <summary>Validates the operational cache against terminal, branch, schema, and freshness policy.</summary>
    Task<(CacheValidationStatus Status, string Message)> ValidateCacheAsync(
        Guid branchId,
        Guid terminalId,
        CacheFreshnessPolicy policy,
        CancellationToken cancellationToken = default);
}
