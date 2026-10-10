namespace Clovent.Platform.Sync;

/// <summary>Status of the synchronization handshake between terminals and branch replication hub.</summary>
public sealed record SyncHandshakeStatus(
    DateTimeOffset? LastHandshakeUtc,
    bool IsReachable,
    string CircuitBreakerState,
    int ConsecutiveFailures,
    long TotalPacketsDispatched);

/// <summary>Result returned after attempting to dispatch sync packets.</summary>
public sealed record SyncDispatchResult(
    bool Success,
    int DeliveredCount,
    string? ErrorMessage = null,
    bool CircuitOpen = false,
    DateTimeOffset HandshakeTimestampUtc = default,
    bool IsOffline = false)
{
    /// <summary>Successful dispatch result.</summary>
    public static SyncDispatchResult Succeeded(int count, DateTimeOffset handshakeUtc)
        => new(true, count, null, false, handshakeUtc, false);

    /// <summary>Circuit breaker open dispatch result.</summary>
    public static SyncDispatchResult CircuitBreakerOpen(string error)
        => new(false, 0, error, true, DateTimeOffset.UtcNow, false);

    /// <summary>Network offline dispatch result.</summary>
    public static SyncDispatchResult NetworkOffline(string error)
        => new(false, 0, error, false, DateTimeOffset.UtcNow, true);

    /// <summary>Failed dispatch result.</summary>
    public static SyncDispatchResult Failed(string error)
        => new(false, 0, error, false, DateTimeOffset.UtcNow, false);
}

/// <summary>
/// Dispatcher engine responsible for pushing delta synchronization packets
/// across network boundaries under circuit breaker and connectivity protection.
/// </summary>
public interface ISyncPacketDispatcher
{
    /// <summary>Dispatches a single synchronization packet to the target endpoint.</summary>
    Task<SyncDispatchResult> DispatchPacketAsync(SyncPacket packet, CancellationToken cancellationToken = default);

    /// <summary>Dispatches a batch of synchronization packets to the target endpoint.</summary>
    Task<SyncDispatchResult> DispatchBatchAsync(IReadOnlyList<SyncPacket> packets, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the current handshake, network, and circuit breaker status.</summary>
    SyncHandshakeStatus GetHandshakeStatus();

    /// <summary>Manually resets the circuit breaker protecting delta sync dispatch.</summary>
    void ResetCircuitBreaker();

    /// <summary>Explicitly records a successful handshake with the remote endpoint.</summary>
    void RecordHandshake(DateTimeOffset timestampUtc);
}
