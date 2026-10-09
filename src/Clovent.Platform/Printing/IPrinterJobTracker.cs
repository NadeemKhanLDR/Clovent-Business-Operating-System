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

    /// <summary>Transitions job status to InFlight (actively being dispatched to spooler or hardware port).</summary>
    void MarkInFlight(Guid jobId);

    /// <summary>Transitions job status to Submitted (accepted by spooler).</summary>
    void MarkSubmitted(Guid jobId);

    /// <summary>Transitions job status to Confirmed (hardware completed).</summary>
    void MarkConfirmed(Guid jobId);

    /// <summary>Transitions job status to Failed with actionable error text.</summary>
    void MarkFailed(Guid jobId, string errorMessage);

    /// <summary>Transitions job status to Cancelled.</summary>
    void MarkCancelled(Guid jobId);

    /// <summary>Transitions job status to Ambiguous when submission outcome cannot be verified.</summary>
    void MarkAmbiguous(Guid jobId, string reason);

    /// <summary>Retrieves all unresolved jobs (Requested, Queued, InFlight, Ambiguous) pending operator resolution.</summary>
    IReadOnlyList<PrinterJob> GetUnresolvedJobs();

    /// <summary>
    /// Gets the number of times a document reference (e.g. order number) has been reprinted, scoped optionally by organization.
    /// Survives application restarts when backed by durable storage.
    /// </summary>
    int GetReprintCount(string documentReference, Guid? organizationId = null);

    /// <summary>
    /// Gets the number of times an immutable document identifier (e.g. OrderId or RefundId) has been reprinted, scoped optionally by organization.
    /// Survives application restarts when backed by durable storage.
    /// </summary>
    int GetReprintCount(Guid documentId, Guid? organizationId = null);
}
