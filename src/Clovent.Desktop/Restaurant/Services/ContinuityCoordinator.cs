using Clovent.Restaurant.Application.Continuity;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Services;

/// <summary>
/// Singleton coordinator for offline Continuity Mode.
/// Manages terminal continuity state, monitors database availability, and coordinates journal recording and replay.
/// </summary>
public sealed class ContinuityCoordinator : IContinuityCoordinator
{
    private readonly IContinuityJournalStore _journalStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContinuityCoordinator>? _logger;

    private readonly object _stateLock = new();
    private bool _isActive;
    private string? _reason;
    private DateTimeOffset? _enteredAtUtc;

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
    public event EventHandler<ContinuityStateChangedEventArgs>? StateChanged;

    /// <summary>Initializes a new instance of <see cref="ContinuityCoordinator"/>.</summary>
    public ContinuityCoordinator(
        IContinuityJournalStore journalStore,
        IServiceScopeFactory scopeFactory,
        ILogger<ContinuityCoordinator>? logger = null)
    {
        _journalStore = journalStore;
        _scopeFactory = scopeFactory;
        _logger = logger;
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
            StateChanged?.Invoke(this, new ContinuityStateChangedEventArgs(true, reason));
        }
    }

    /// <inheritdoc />
    public void ExitContinuityMode()
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
                _logger?.LogInformation("Exited Emergency Continuity Mode. Resuming normal online operations.");
            }
        }

        if (changed)
        {
            StateChanged?.Invoke(this, new ContinuityStateChangedEventArgs(false, "Normal operations resumed."));
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

            return await db.Database.CanConnectAsync(cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Database connectivity check failed.");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<EmergencyTransaction> RecordEmergencySaleAsync(
        EmergencyTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _journalStore.AppendAsync(transaction, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("Recorded emergency sale {TransactionId} (Sequence: {Seq}) in local journal",
            transaction.TransactionId, transaction.SequenceNumber);

        return transaction;
    }

    /// <inheritdoc />
    public async Task<EmergencyReplayResult> TriggerReplayAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var replayer = scope.ServiceProvider.GetRequiredService<IEmergencyJournalReplayer>();

        var result = await replayer.ReplayPendingAsync(cancellationToken).ConfigureAwait(false);

        if (result.PendingCountAfterReplay() == 0 && IsContinuityModeActive)
        {
            // If DB is healthy and all pending items replayed, exit continuity mode
            var isDbUp = await CheckDatabaseHealthAsync(cancellationToken).ConfigureAwait(false);
            if (isDbUp)
            {
                ExitContinuityMode();
            }
        }

        return result;
    }

    /// <inheritdoc />
    public Task<ContinuityJournalStatistics> GetJournalStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return _journalStore.GetStatisticsAsync(cancellationToken);
    }
}

internal static class ReplayResultExtensions
{
    public static int PendingCountAfterReplay(this EmergencyReplayResult result)
    {
        return result.TotalProcessed - result.SuccessCount - result.DuplicateIgnoredCount;
    }
}
