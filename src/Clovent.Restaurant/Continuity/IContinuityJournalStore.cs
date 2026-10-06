namespace Clovent.Restaurant.Continuity;

/// <summary>Summary statistics of the emergency offline transaction journal.</summary>
public sealed record ContinuityJournalStatistics(
    int TotalRecorded,
    int PendingReplayCount,
    int ReplayedCount,
    int FailedOrConflictCount,
    DateTimeOffset? OldestPendingAtUtc);

/// <summary>Storage abstraction for the local encrypted emergency transaction journal.</summary>
public interface IContinuityJournalStore
{
    /// <summary>Appends a new emergency transaction to the protected local journal.</summary>
    Task AppendAsync(EmergencyTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all emergency transactions recorded in the journal.</summary>
    Task<IReadOnlyList<EmergencyTransaction>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves all emergency transactions that are pending replay to the primary database.</summary>
    Task<IReadOnlyList<EmergencyTransaction>> GetPendingReplayAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates the reconciliation state of a journaled transaction.</summary>
    Task UpdateAsync(EmergencyTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Retrieves aggregate statistics of the local journal.</summary>
    Task<ContinuityJournalStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the next sequential sequence number for this terminal.</summary>
    Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the cryptographic hash or HMAC signature of the last recorded transaction in the chain.</summary>
    Task<string> GetLastTransactionHashAsync(CancellationToken cancellationToken = default);
}
