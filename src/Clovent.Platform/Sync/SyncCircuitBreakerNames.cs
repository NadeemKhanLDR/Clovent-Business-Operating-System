namespace Clovent.Platform.Sync;

/// <summary>Standard circuit breaker names used for replication and delta synchronization.</summary>
public static class SyncCircuitBreakerNames
{
    /// <summary>Circuit breaker protecting against repeated failures during multi-terminal branch delta sync push.</summary>
    public const string BranchDeltaSync = "BranchDeltaSync";
}
