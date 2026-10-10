namespace Clovent.Platform.Printing;

/// <summary>
/// Tracks print job lifecycle transitions, reprint history, and failure diagnostics.
/// </summary>
public interface IPrinterJobTracker
{
    /// <summary>Records a newly created print job.</summary>
    void RecordJob(PrinterJob job);

    /// <summary>Gets a tracked job by its identifier.</summary>
    PrinterJob? GetJob(Guid jobId);

    /// <summary>Returns the most recent print jobs up to the specified limit.</summary>
    IReadOnlyList<PrinterJob> GetRecentJobs(int limit = 50);

    /// <summary>Returns all unresolved print jobs (Requested, Queued, InFlight, or Ambiguous) that require attention or recovery.</summary>
    IReadOnlyList<PrinterJob> GetUnresolvedJobs();

    /// <summary>Marks a job as queued for dispatch.</summary>
    void MarkQueued(Guid jobId);

    /// <summary>Marks a job as actively in-flight immediately prior to OS spooler/adapter invocation.</summary>
    void MarkInFlight(Guid jobId);

    /// <summary>Marks a job as submitted to the print spooler or adapter.</summary>
    void MarkSubmitted(Guid jobId);

    /// <summary>Marks a job as confirmed completed.</summary>
    void MarkConfirmed(Guid jobId);

    /// <summary>Marks a job as failed with an actionable error message.</summary>
    void MarkFailed(Guid jobId, string errorMessage);

    /// <summary>Marks a job as cancelled.</summary>
    void MarkCancelled(Guid jobId);

    /// <summary>Marks a job as ambiguous (e.g. interrupted across restart after spooler submission).</summary>
    void MarkAmbiguous(Guid jobId, string reason);

    /// <summary>Marks a job as quarantined to durable storage due to hardware fault or open circuit breaker.</summary>
    void MarkQuarantined(Guid jobId, string errorMessage);

    /// <summary>Gets the number of reprints recorded for a specific document reference, optionally scoped by organization.</summary>
    int GetReprintCount(string documentReference, Guid? organizationId = null);

    /// <summary>Gets the number of reprints recorded for an immutable document identifier, optionally scoped by organization.</summary>
    int GetReprintCount(Guid documentId, Guid? organizationId = null);
}
