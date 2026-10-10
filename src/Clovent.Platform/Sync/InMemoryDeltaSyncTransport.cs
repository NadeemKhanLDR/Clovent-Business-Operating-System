using System.Collections.Concurrent;

namespace Clovent.Platform.Sync;

/// <summary>
/// In-memory loopback and peer transport for delta synchronization packets.
/// Ideal for testing, unit/integration verification, and single-host multi-terminal simulations.
/// </summary>
public sealed class InMemoryDeltaSyncTransport : IDeltaSyncTransport
{
    private readonly ConcurrentBag<SyncPacket> _deliveredPackets = new();
    private Func<SyncPacket, CancellationToken, Task<bool>>? _packetHandler;
    private volatile bool _simulateFailure;
    private int _failCountRemaining;

    /// <summary>Delivered packets history.</summary>
    public IReadOnlyCollection<SyncPacket> DeliveredPackets => _deliveredPackets.ToArray();

    /// <summary>Configures an asynchronous ingestion handler callback when packets arrive.</summary>
    public void SetPacketHandler(Func<SyncPacket, CancellationToken, Task<bool>> handler)
    {
        _packetHandler = handler;
    }

    /// <summary>Simulates transport failures for testing circuit breaker behavior.</summary>
    public void SetSimulateFailure(bool simulateFailure)
    {
        _simulateFailure = simulateFailure;
    }

    /// <summary>Simulates a specific number of consecutive failure calls.</summary>
    public void SetFailNextCalls(int count)
    {
        _failCountRemaining = count;
    }

    /// <inheritdoc/>
    public async Task<SyncTransportResult> SendPacketAsync(SyncPacket packet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packet);
        return await SendBatchAsync([packet], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SyncTransportResult> SendBatchAsync(IReadOnlyList<SyncPacket> packets, CancellationToken cancellationToken = default)
    {
        if (packets == null || packets.Count == 0)
        {
            return SyncTransportResult.Succeeded(0);
        }

        if (_simulateFailure)
        {
            return SyncTransportResult.Failed("Simulated network/transport failure.");
        }

        if (_failCountRemaining > 0)
        {
            Interlocked.Decrement(ref _failCountRemaining);
            return SyncTransportResult.Failed("Simulated intermittent connection drop.");
        }

        foreach (var packet in packets)
        {
            if (_packetHandler != null)
            {
                var accepted = await _packetHandler(packet, cancellationToken).ConfigureAwait(false);
                if (!accepted)
                {
                    return SyncTransportResult.Failed($"Packet {packet.PacketId} was rejected by receiver.");
                }
            }

            _deliveredPackets.Add(packet);
        }

        return SyncTransportResult.Succeeded(packets.Count);
    }

    /// <summary>Clears the delivered packet history.</summary>
    public void Clear()
    {
        _deliveredPackets.Clear();
    }
}
