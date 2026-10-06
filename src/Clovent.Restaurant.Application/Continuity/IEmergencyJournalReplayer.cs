namespace Clovent.Restaurant.Application.Continuity;

/// <summary>Result summary returned after replaying offline emergency transactions.</summary>
public sealed record EmergencyReplayResult(
    int TotalProcessed,
    int SuccessCount,
    int DuplicateIgnoredCount,
    int FailedCount,
    IReadOnlyList<string> Errors);

/// <summary>Contract for replaying emergency transactions recorded offline into the primary database.</summary>
public interface IEmergencyJournalReplayer
{
    /// <summary>Replays all pending emergency transactions from the local journal into the primary database.</summary>
    Task<EmergencyReplayResult> ReplayPendingAsync(CancellationToken cancellationToken = default);
}
