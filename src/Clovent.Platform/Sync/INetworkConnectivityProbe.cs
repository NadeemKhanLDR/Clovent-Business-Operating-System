namespace Clovent.Platform.Sync;

/// <summary>
/// Probes network connectivity for branch replication and delta-sync push.
/// Supports runtime network presence detection and testing simulation.
/// </summary>
public interface INetworkConnectivityProbe
{
    /// <summary>Whether network connectivity is currently active.</summary>
    bool IsConnected { get; }

    /// <summary>Asynchronously tests active network connectivity to the branch hub or sync endpoint.</summary>
    Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default);

    /// <summary>Explicitly sets the simulated connectivity state (useful for offline testing and continuity simulation).</summary>
    void SetConnected(bool isConnected);

    /// <summary>Event raised whenever connectivity transitions between online and offline.</summary>
    event EventHandler<bool>? ConnectivityChanged;
}

/// <summary>Thread-safe in-memory network connectivity probe.</summary>
public sealed class NetworkConnectivityProbe : INetworkConnectivityProbe
{
    private volatile bool _isConnected;
    private readonly object _lock = new();

    /// <summary>Creates a new probe with the initial connected state (defaults to true).</summary>
    public NetworkConnectivityProbe(bool initialConnected = true)
    {
        _isConnected = initialConnected;
    }

    /// <inheritdoc/>
    public bool IsConnected => _isConnected;

    /// <inheritdoc/>
    public event EventHandler<bool>? ConnectivityChanged;

    /// <inheritdoc/>
    public Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_isConnected);
    }

    /// <inheritdoc/>
    public void SetConnected(bool isConnected)
    {
        bool changed = false;
        lock (_lock)
        {
            if (_isConnected != isConnected)
            {
                _isConnected = isConnected;
                changed = true;
            }
        }

        if (changed)
        {
            ConnectivityChanged?.Invoke(this, isConnected);
        }
    }
}
