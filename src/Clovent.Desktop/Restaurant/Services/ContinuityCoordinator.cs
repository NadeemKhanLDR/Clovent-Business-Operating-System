using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Restaurant.Application.Continuity;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Services;

/// <summary>
/// Singleton coordinator for offline Continuity Mode and Local Operational Cache.
/// Manages terminal continuity state, monitors database availability, coordinates journal recording,
/// executes background cache synchronization, and ensures safe atomic replay upon database restoration.
/// </summary>
public sealed class ContinuityCoordinator : IContinuityCoordinator
{
    private readonly IContinuityJournalStore _journalStore;
    private readonly IOperationalCacheStore _cacheStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContinuityCoordinator>? _logger;
    private readonly SemaphoreSlim _replayGate = new(1, 1);

    private readonly object _stateLock = new();
    private bool _isActive;
    private string? _reason;
    private DateTimeOffset? _enteredAtUtc;

    private OperationalCacheSnapshot? _activeCache;
    private CacheValidationStatus _cacheStatus = CacheValidationStatus.Missing;
    private string? _cacheStatusMessage = "Operational cache not initialized.";
    private DateTimeOffset? _lastCacheSyncUtc;

    // Terminal Context
    private Guid _companyId;
    private string _companyName = "Default Company";
    private Guid _branchId;
    private string _branchName = "Default Branch";
    private Guid _terminalId;
    private string _terminalName = "Default Terminal";
    private string _terminalCode = "POS01";
    private Guid _warehouseId;
    private string _warehouseName = "Main Warehouse";
    private string _currencyCode = "PKR";
    private string _currencySymbol = "Rs.";
    private int _currencyDecimals = 2;

    /// <inheritdoc />
    public bool IsContinuityModeActive
    {
        get { lock (_stateLock) return _isActive; }
    }

    /// <inheritdoc />
    public string? ContinuityReason
    {
        get { lock (_stateLock) return _reason; }
    }

    /// <inheritdoc />
    public DateTimeOffset? ContinuityEnteredAtUtc
    {
        get { lock (_stateLock) return _enteredAtUtc; }
    }

    /// <inheritdoc />
    public IOperationalCacheStore CacheStore => _cacheStore;

    /// <inheritdoc />
    public OperationalCacheSnapshot? ActiveCache
    {
        get { lock (_stateLock) return _activeCache; }
    }

    /// <inheritdoc />
    public CacheValidationStatus CacheStatus
    {
        get { lock (_stateLock) return _cacheStatus; }
    }

    /// <inheritdoc />
    public string? CacheStatusMessage
    {
        get { lock (_stateLock) return _cacheStatusMessage; }
    }

    /// <inheritdoc />
    public DateTimeOffset? LastCacheSyncUtc
    {
        get { lock (_stateLock) return _lastCacheSyncUtc; }
    }

    /// <inheritdoc />
    public Guid CurrentTerminalId
    {
        get { lock (_stateLock) return _terminalId; }
    }

    /// <inheritdoc />
    public Guid CurrentBranchId
    {
        get { lock (_stateLock) return _branchId; }
    }

    /// <inheritdoc />
    public string CurrentTerminalCode
    {
        get { lock (_stateLock) return _terminalCode; }
    }

    private long _offlineSequenceCounter;

    /// <inheritdoc />
    public string GetNextLocalReceiptNumber(long? sequence = null)
    {
        var seq = sequence ?? Interlocked.Increment(ref _offlineSequenceCounter);
        return LocalReceiptNumber.Generate(_terminalCode, seq);
    }

    /// <inheritdoc />
    public event EventHandler<ContinuityStateChangedEventArgs>? StateChanged;

    /// <summary>Initializes a new instance of <see cref="ContinuityCoordinator"/>.</summary>
    public ContinuityCoordinator(
        IContinuityJournalStore journalStore,
        IOperationalCacheStore cacheStore,
        IServiceScopeFactory scopeFactory,
        ILogger<ContinuityCoordinator>? logger = null)
    {
        _journalStore = journalStore ?? throw new ArgumentNullException(nameof(journalStore));
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger;
    }

    /// <inheritdoc />
    public void SetTerminalContext(
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
        int currencyDecimals)
    {
        lock (_stateLock)
        {
            _companyId = companyId;
            _companyName = companyName;
            _branchId = branchId;
            _branchName = branchName;
            _terminalId = terminalId;
            _terminalName = terminalName;
            _terminalCode = terminalCode;
            _warehouseId = warehouseId;
            _warehouseName = warehouseName;
            _currencyCode = currencyCode;
            _currencySymbol = currencySymbol;
            _currencyDecimals = currencyDecimals;
        }

        _logger?.LogInformation(
            "Terminal context set on ContinuityCoordinator: {TerminalCode} ({TerminalId}) at branch {BranchName}",
            terminalCode, terminalId, branchName);

        // Best effort async load of existing local cache
        _ = Task.Run(async () =>
        {
            try
            {
                await EnsureCacheLoadedAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Background initial cache load encountered an issue");
            }
        });
    }

    /// <inheritdoc />
    public void EnterContinuityMode(string reason)
    {
        bool changed = false;
        lock (_stateLock)
        {
            if (!_isActive)
            {
                _isActive = true;
                _reason = reason;
                _enteredAtUtc = DateTimeOffset.UtcNow;
                changed = true;
                _logger?.LogWarning("Entered Emergency Continuity Mode: {Reason}", reason);
            }
        }

        if (changed)
        {
            SmartPosLayoutTelemetry.LogContinuityEvent(
                "EnterContinuityMode",
                _terminalCode,
                _terminalId,
                true,
                reason);

            StateChanged?.Invoke(this, new ContinuityStateChangedEventArgs(true, reason));
        }
    }

    /// <inheritdoc />
    public void ExitContinuityMode(int replayedCount = 0)
    {
        bool changed = false;
        lock (_stateLock)
        {
            if (_isActive)
            {
                _isActive = false;
                _reason = null;
                _enteredAtUtc = null;
                changed = true;
                _logger?.LogInformation("Exited Emergency Continuity Mode. Resuming normal online operations. Replayed: {Count}", replayedCount);
            }
        }

        if (changed)
        {
            SmartPosLayoutTelemetry.LogContinuityEvent(
                "ExitContinuityMode",
                _terminalCode,
                _terminalId,
                false,
                "Normal operations resumed.",
                replayedCount: replayedCount);

            StateChanged?.Invoke(this, new ContinuityStateChangedEventArgs(false, "Normal operations resumed.", replayedCount));
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<RestaurantDbContext>();
            if (db == null) return false;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            var isUp = await db.Database.CanConnectAsync(cts.Token).ConfigureAwait(false);
            return isUp;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Database connectivity check failed.");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        if (_terminalId == Guid.Empty)
        {
            _logger?.LogWarning("Cannot refresh cache: Terminal context has not been configured yet.");
            return false;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<IOperationalCacheSynchronizer>();

            var snapshot = await synchronizer.SynchronizeAsync(
                _companyId,
                _companyName,
                _branchId,
                _branchName,
                _terminalId,
                _terminalName,
                _terminalCode,
                _warehouseId,
                _warehouseName,
                _currencyCode,
                _currencySymbol,
                _currencyDecimals,
                cancellationToken).ConfigureAwait(false);

            lock (_stateLock)
            {
                _activeCache = snapshot;
                _cacheStatus = CacheValidationStatus.Valid;
                _cacheStatusMessage = "Operational cache synchronized successfully.";
                _lastCacheSyncUtc = snapshot.Metadata.LastSuccessfulSyncUtc;
            }

            _logger?.LogInformation(
                "Operational cache refreshed successfully for terminal {TerminalCode} [Version: {Version}]",
                _terminalCode, snapshot.Metadata.CacheVersion);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to refresh operational cache for terminal {TerminalId}", _terminalId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<(CacheValidationStatus Status, string Message)> ValidateCacheAsync(CancellationToken cancellationToken = default)
    {
        if (_terminalId == Guid.Empty)
        {
            return (CacheValidationStatus.Missing, "No terminal context configured.");
        }

        try
        {
            var snapshot = await _cacheStore.LoadSnapshotAsync(_terminalId, cancellationToken).ConfigureAwait(false);
            if (snapshot == null)
            {
                lock (_stateLock)
                {
                    _cacheStatus = CacheValidationStatus.Missing;
                    _cacheStatusMessage = "No operational cache snapshot found on this workstation.";
                }
                return (_cacheStatus, _cacheStatusMessage);
            }

            var (status, message) = snapshot.Validate(_branchId, _terminalId, CacheFreshnessPolicy.Default);
            lock (_stateLock)
            {
                _cacheStatus = status;
                _cacheStatusMessage = message;
                _lastCacheSyncUtc = snapshot.Metadata.LastSuccessfulSyncUtc;
                if (status is CacheValidationStatus.Valid or CacheValidationStatus.StaleWithinPolicy)
                {
                    _activeCache = snapshot;
                }
            }
            return (status, message);
        }
        catch (ContinuityTamperException ex)
        {
            lock (_stateLock)
            {
                _cacheStatus = CacheValidationStatus.Tampered;
                _cacheStatusMessage = ex.Message;
            }
            return (CacheValidationStatus.Tampered, ex.Message);
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                _cacheStatus = CacheValidationStatus.Corrupt;
                _cacheStatusMessage = $"Cache load error: {ex.Message}";
            }
            return (CacheValidationStatus.Corrupt, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<OperationalCacheSnapshot?> EnsureCacheLoadedAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            if (_activeCache != null && _cacheStatus is CacheValidationStatus.Valid or CacheValidationStatus.StaleWithinPolicy)
            {
                return _activeCache;
            }
        }

        if (_terminalId == Guid.Empty) return null;

        var (status, _) = await ValidateCacheAsync(cancellationToken).ConfigureAwait(false);
        lock (_stateLock)
        {
            if (status is CacheValidationStatus.Valid or CacheValidationStatus.StaleWithinPolicy)
            {
                return _activeCache;
            }
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<EmergencyTransaction> RecordEmergencySaleAsync(
        EmergencyTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _journalStore.AppendAsync(transaction, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("Recorded emergency sale {TransactionId} (LocalReceipt: {Receipt}, Sequence: {Seq}) in local journal",
            transaction.TransactionId, transaction.LocalReceiptNumber, transaction.SequenceNumber);

        SmartPosLayoutTelemetry.LogContinuityEvent(
            "RecordEmergencySale",
            _terminalCode,
            _terminalId,
            IsContinuityModeActive,
            $"Offline cash sale {transaction.LocalReceiptNumber ?? transaction.TransactionId.ToString()} recorded",
            details: $"Seq: {transaction.SequenceNumber}, Total: {transaction.OrderSnapshot?.GrandTotal:F2}");

        return transaction;
    }

    /// <inheritdoc />
    public async Task<EmergencyReplayResult> TriggerReplayAsync(CancellationToken cancellationToken = default)
    {
        await _replayGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var replayer = scope.ServiceProvider.GetRequiredService<IEmergencyJournalReplayer>();

            var result = await replayer.ReplayPendingAsync(cancellationToken).ConfigureAwait(false);

            if (result.FailedCount == 0 && IsContinuityModeActive)
            {
                // If DB is healthy and all pending items replayed, synchronize cache and exit continuity mode
                var isDbUp = await CheckDatabaseHealthAsync(cancellationToken).ConfigureAwait(false);
                if (isDbUp)
                {
                    _ = RefreshCacheAsync(cancellationToken);
                    ExitContinuityMode(result.SuccessCount);
                }
            }

            SmartPosLayoutTelemetry.LogContinuityEvent(
                "TriggerReplayCompleted",
                _terminalCode,
                _terminalId,
                IsContinuityModeActive,
                result.FailedCount > 0 ? $"Replay completed with {result.FailedCount} failures" : "Replay completed successfully",
                pendingJournalCount: result.TotalProcessed - result.SuccessCount - result.DuplicateIgnoredCount,
                replayedCount: result.SuccessCount,
                details: $"Total: {result.TotalProcessed}, Success: {result.SuccessCount}, DuplicateIgnored: {result.DuplicateIgnoredCount}, Failed: {result.FailedCount}");

            return result;
        }
        finally
        {
            _replayGate.Release();
        }
    }

    /// <inheritdoc />
    public Task<ContinuityJournalStatistics> GetJournalStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return _journalStore.GetStatisticsAsync(cancellationToken);
    }
}
