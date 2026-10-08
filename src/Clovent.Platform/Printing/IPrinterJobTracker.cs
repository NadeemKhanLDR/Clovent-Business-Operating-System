namespace Clovent.Platform.Printing;

/// <summary>
/// Tracks in-flight and historical print jobs for observability and recovery.
/// </summary>
public interface IPrinterJobTracker
{
    /// <summary>Registers a new print job in Requested status.</summary>
    void RecordJob(PrinterJob job);

    /// <summary>Retrieves a job by its unique identifier.</summary>
    PrinterJob? GetJob(Guid jobId);

    /// <summary>Retrieves recently dispatched jobs ordered by creation timestamp descending.</summary>
    IReadOnlyList<PrinterJob> GetRecentJobs(int limit = 50);

    /// <summary>Transitions job status to Queued.</summary>
    void MarkQueued(Guid jobId);

    /// <summary>Transitions job status to Submitted (accepted by spooler).</summary>
    void MarkSubmitted(Guid jobId);

    /// <summary>Transitions job status to Confirmed (hardware completed).</summary>
    void MarkConfirmed(Guid jobId);

    /// <summary>Transitions job status to Failed with actionable error text.</summary>
    void MarkFailed(Guid jobId, string errorMessage);

    /// <summary>Transitions job status to Cancelled.</summary>
    void MarkCancelled(Guid jobId);
}
