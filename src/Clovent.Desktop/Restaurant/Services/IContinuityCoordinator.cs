using Clovent.Restaurant.Application.Continuity;
using Clovent.Restaurant.Continuity;

namespace Clovent.Desktop.Restaurant.Services;

/// <summary>Event args published when continuity mode status changes.</summary>
public sealed class ContinuityStateChangedEventArgs : EventArgs
{
    /// <summary>Whether continuity mode is now active.</summary>
    public bool IsActive { get; }

    /// <summary>Reason why continuity mode was activated or exited.</summary>
    public string? Reason { get; }

    /// <summary>Number of transactions replayed during restoration, if applicable.</summary>
    public int ReplayedCount { get; }

    /// <summary>Initializes a new instance of <see cref="ContinuityStateChangedEventArgs"/>.</summary>
    public ContinuityStateChangedEventArgs(bool isActive, string? reason, int replayedCount = 0)
    {
        IsActive = isActive;
        Reason = reason;
        ReplayedCount = replayedCount;
    }
}

/// <summary>Coordinates POS terminal continuity mode state, health checking, operational cache, and offline transaction recording.</summary>
public interface IContinuityCoordinator
{
    /// <summary>Indicates whether the counter is currently operating in emergency offline Continuity Mode.</summary>
    bool IsContinuityModeActive { get; }

    /// <summary>Reason why Continuity Mode was entered.</summary>
    string? ContinuityReason { get; }

    /// <summary>UTC timestamp when Continuity Mode was activated.</summary>
    DateTimeOffset? ContinuityEnteredAtUtc { get; }

    /// <summary>Local operational cache store instance.</summary>
    IOperationalCacheStore CacheStore { get; }

    /// <summary>Active in-memory operational cache snapshot, if loaded.</summary>
    OperationalCacheSnapshot? ActiveCache { get; }

    /// <summary>Current validation status of the local operational cache.</summary>
    CacheValidationStatus CacheStatus { get; }

    /// <summary>Human-readable detail message for current cache status.</summary>
    string? CacheStatusMessage { get; }

    /// <summary>Timestamp of last successful cache synchronization.</summary>
    DateTimeOffset? LastCacheSyncUtc { get; }

    /// <summary>Current configured terminal identity.</summary>
    Guid CurrentTerminalId { get; }

    /// <summary>Current configured branch identity.</summary>
    Guid CurrentBranchId { get; }

    /// <summary>Current configured terminal code.</summary>
    string CurrentTerminalCode { get; }

    /// <summary>Generates the next human-readable collision-free local receipt number (e.g. CONT-POS01-00001).</summary>
    string GetNextLocalReceiptNumber(long? sequence = null);

    /// <summary>Event raised whenever Continuity Mode is entered or exited.</summary>
    event EventHandler<ContinuityStateChangedEventArgs>? StateChanged;

    /// <summary>Configures workstation terminal and branch execution context.</summary>
    void SetTerminalContext(
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
        int currencyDecimals);

    /// <summary>Manually or automatically transitions counter into emergency Continuity Mode.</summary>
    void EnterContinuityMode(string reason);

    /// <summary>Transitions counter back to normal online operations.</summary>
    void ExitContinuityMode(int replayedCount = 0);

    /// <summary>Performs a live connectivity test against the primary database.</summary>
    Task<bool> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>Refreshes the local operational cache from the database asynchronously.</summary>
    Task<bool> RefreshCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates the integrity, freshness, and terminal match of the local operational cache.</summary>
    Task<(CacheValidationStatus Status, string Message)> ValidateCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads the local operational cache into memory.</summary>
    Task<OperationalCacheSnapshot?> EnsureCacheLoadedAsync(CancellationToken cancellationToken = default);

    /// <summary>Records an emergency sale in the local encrypted journal.</summary>
    Task<EmergencyTransaction> RecordEmergencySaleAsync(EmergencyTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Triggers immediate replay of all pending offline emergency transactions to the database.</summary>
    Task<EmergencyReplayResult> TriggerReplayAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves statistics from the offline journal.</summary>
    Task<ContinuityJournalStatistics> GetJournalStatisticsAsync(CancellationToken cancellationToken = default);
}
