using Clovent.Platform.CircuitBreakers;
using Microsoft.Extensions.Logging;

namespace Clovent.Platform.Sync;

/// <summary>
/// Resilient delta synchronization dispatcher.
/// Coordinates with <see cref="INetworkConnectivityProbe"/> and <see cref="ICircuitBreaker"/>
/// to ensure immediate background dispatch when connected, and non-blocking autonomous offline queueing.
/// </summary>
public sealed class DeltaSyncDispatcher : ISyncPacketDispatcher
{
    private readonly IDeltaSyncTransport _transport;
    private readonly INetworkConnectivityProbe _connectivityProbe;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ILogger<DeltaSyncDispatcher> _logger;

    private readonly object _stateLock = new();
    private DateTimeOffset? _lastHandshakeUtc;
    private long _totalPacketsDispatched;

    /// <summary>Creates a new delta-sync dispatcher.</summary>
    public DeltaSyncDispatcher(
        IDeltaSyncTransport transport,
        INetworkConnectivityProbe connectivityProbe,
        ICircuitBreakerRegistry circuitBreakerRegistry,
        ILogger<DeltaSyncDispatcher> logger)
    {
        _transport = transport;
        _connectivityProbe = connectivityProbe;
        _logger = logger;

        // Register or retrieve circuit breaker with 3 failures and 30s break duration
        _circuitBreaker = circuitBreakerRegistry.GetOrCreate(
            SyncCircuitBreakerNames.BranchDeltaSync,
            new CircuitBreakerOptions
            {
                FailureThreshold = 3,
                OpenDuration = TimeSpan.FromSeconds(30),
                HalfOpenSuccessThreshold = 1
            });
    }

    /// <inheritdoc/>
    public async Task<SyncDispatchResult> DispatchPacketAsync(SyncPacket packet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packet);
        return await DispatchBatchAsync([packet], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SyncDispatchResult> DispatchBatchAsync(IReadOnlyList<SyncPacket> packets, CancellationToken cancellationToken = default)
    {
        if (packets == null || packets.Count == 0)
        {
            return SyncDispatchResult.Succeeded(0, _lastHandshakeUtc ?? DateTimeOffset.UtcNow);
        }

        // 1. Fast check: is network probe reporting active connection?
        if (!_connectivityProbe.IsConnected)
        {
            _logger.LogDebug("Network connectivity is offline; delta sync packets deferred for autonomous queueing ({Count} packets).", packets.Count);
            return SyncDispatchResult.NetworkOffline("Network connectivity is offline (currently inactive). Packets queued for autonomous push.");
        }

        // 2. Circuit breaker check
        if (!_circuitBreaker.CanExecute())
        {
            _logger.LogWarning("Delta-sync circuit breaker is OPEN ({BreakerName}). Suppressing push attempts.", _circuitBreaker.Name);
            return SyncDispatchResult.CircuitBreakerOpen($"Circuit breaker '{_circuitBreaker.Name}' is open. Next attempt in cooldown.");
        }

        try
        {
            // 3. Protected transport execution
            var transportResult = await _circuitBreaker.ExecuteAsync(async () =>
            {
                var result = await _transport.SendBatchAsync(packets, cancellationToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorMessage ?? "Transport failed to deliver sync packets.");
                }
                return result;
            }, cancellationToken).ConfigureAwait(false);

            var now = DateTimeOffset.UtcNow;
            RecordHandshake(now);

            lock (_stateLock)
            {
                _totalPacketsDispatched += transportResult.DeliveredCount;
            }

            _logger.LogInformation("Successfully dispatched {Count} delta sync packets to replication target. Handshake recorded at {Timestamp}.",
                transportResult.DeliveredCount, now);

            return SyncDispatchResult.Succeeded(transportResult.DeliveredCount, now);
        }
        catch (CircuitBreakerOpenException cbEx)
        {
            _logger.LogWarning(cbEx, "Circuit breaker tripped during delta-sync dispatch.");
            return SyncDispatchResult.CircuitBreakerOpen(cbEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispatch batch of {Count} delta-sync packets.", packets.Count);
            return SyncDispatchResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc/>
    public SyncHandshakeStatus GetHandshakeStatus()
    {
        lock (_stateLock)
        {
            return new SyncHandshakeStatus(
                LastHandshakeUtc: _lastHandshakeUtc,
                IsReachable: _connectivityProbe.IsConnected && _circuitBreaker.State != CircuitBreakerState.Open,
                CircuitBreakerState: _circuitBreaker.State.ToString(),
                ConsecutiveFailures: _circuitBreaker.FailureCount,
                TotalPacketsDispatched: _totalPacketsDispatched);
        }
    }

    /// <inheritdoc/>
    public void ResetCircuitBreaker()
    {
        _circuitBreaker.Reset();
        _logger.LogInformation("Circuit breaker '{Name}' manually reset.", _circuitBreaker.Name);
    }

    /// <inheritdoc/>
    public void RecordHandshake(DateTimeOffset timestampUtc)
    {
        lock (_stateLock)
        {
            _lastHandshakeUtc = timestampUtc;
        }
    }
}
