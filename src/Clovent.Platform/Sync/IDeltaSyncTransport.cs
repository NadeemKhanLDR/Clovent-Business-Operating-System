namespace Clovent.Platform.Sync;

/// <summary>Result of transporting delta sync packets to the replication receiver.</summary>
public sealed record SyncTransportResult(
    bool Success,
    int DeliveredCount,
    string? ErrorMessage = null)
{
    /// <summary>Creates a successful transport result.</summary>
    public static SyncTransportResult Succeeded(int count) => new(true, count);

    /// <summary>Creates a failed transport result.</summary>
    public static SyncTransportResult Failed(string error) => new(false, 0, error);
}

/// <summary>
/// Transport layer contract delivering sync packets to the target terminal or branch replication hub.
/// </summary>
public interface IDeltaSyncTransport
{
    /// <summary>Sends a single delta synchronization packet to the target endpoint.</summary>
    Task<SyncTransportResult> SendPacketAsync(SyncPacket packet, CancellationToken cancellationToken = default);

    /// <summary>Sends a batch of delta synchronization packets atomically or in a pipelined call.</summary>
    Task<SyncTransportResult> SendBatchAsync(IReadOnlyList<SyncPacket> packets, CancellationToken cancellationToken = default);
}
