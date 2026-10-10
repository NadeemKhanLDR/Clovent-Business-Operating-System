namespace Clovent.Platform.Printing;

/// <summary>
/// Durable storage contract for persisting, enumerating, and managing quarantined print jobs
/// that failed primary hardware dispatch.
/// </summary>
public interface IPrintJobQuarantineStore
{
    /// <summary>Resolved physical directory on disk where quarantined jobs are stored.</summary>
    string QuarantineDirectory { get; }

    /// <summary>Persists a failed print job atomically into durable quarantined storage.</summary>
    Task QuarantineJobAsync(QuarantinedPrintJob job, CancellationToken cancellationToken = default);

    /// <summary>Enumerates all quarantined print jobs currently buffered on disk, ordered newest first.</summary>
    Task<IReadOnlyList<QuarantinedPrintJob>> GetQuarantinedJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves a specific quarantined job by its unique identifier.</summary>
    Task<QuarantinedPrintJob?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>Updates retry metadata on an existing quarantined job.</summary>
    Task UpdateJobAsync(QuarantinedPrintJob job, CancellationToken cancellationToken = default);

    /// <summary>Deletes a quarantined job from disk once it has been successfully recovered or purged.</summary>
    Task<bool> DeleteJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>Returns the current number of quarantined jobs waiting on disk.</summary>
    Task<int> GetQuarantinedJobCountAsync(CancellationToken cancellationToken = default);
}
