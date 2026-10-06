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

    /// <summary>Initializes a new instance of <see cref="ContinuityStateChangedEventArgs"/>.</summary>
    public ContinuityStateChangedEventArgs(bool isActive, string? reason)
    {
        IsActive = isActive;
        Reason = reason;
    }
}

/// <summary>Coordinates POS terminal continuity mode state, health checking, and offline transaction recording.</summary>
public interface IContinuityCoordinator
{
    /// <summary>Indicates whether the counter is currently operating in emergency offline Continuity Mode.</summary>
    bool IsContinuityModeActive { get; }

    /// <summary>Reason why Continuity Mode was entered.</summary>
    string? ContinuityReason { get; }

    /// <summary>UTC timestamp when Continuity Mode was activated.</summary>
    DateTimeOffset? ContinuityEnteredAtUtc { get; }

    /// <summary>Event raised whenever Continuity Mode is entered or exited.</summary>
    event EventHandler<ContinuityStateChangedEventArgs>? StateChanged;

    /// <summary>Manually or automatically transitions counter into emergency Continuity Mode.</summary>
    void EnterContinuityMode(string reason);

    /// <summary>Transitions counter back to normal online operations.</summary>
    void ExitContinuityMode();

    /// <summary>Performs a live connectivity test against the primary database.</summary>
    Task<bool> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>Records an emergency sale in the local encrypted journal.</summary>
    Task<EmergencyTransaction> RecordEmergencySaleAsync(EmergencyTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Triggers immediate replay of all pending offline emergency transactions to the database.</summary>
    Task<EmergencyReplayResult> TriggerReplayAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves statistics from the offline journal.</summary>
    Task<ContinuityJournalStatistics> GetJournalStatisticsAsync(CancellationToken cancellationToken = default);
}
