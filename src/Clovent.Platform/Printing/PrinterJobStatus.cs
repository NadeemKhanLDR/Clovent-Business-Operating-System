namespace Clovent.Platform.Printing;

/// <summary>
/// Tracks the lifecycle state of a print job from creation through spooler dispatch to final confirmation.
/// </summary>
public enum PrinterJobStatus
{
    /// <summary>Job created in application memory; not yet routed.</summary>
    Requested = 0,

    /// <summary>Job validated and queued for target printer profile.</summary>
    Queued = 1,

    /// <summary>Job submitted to the Windows print spooler or hardware adapter.</summary>
    Submitted = 2,

    /// <summary>Spooler or device confirmed receipt/completion.</summary>
    Confirmed = 3,

    /// <summary>Job failed due to missing queue, offline device, or driver error.</summary>
    Failed = 4,

    /// <summary>Job was cancelled by operator or system policy.</summary>
    Cancelled = 5,

    /// <summary>
    /// Job was submitted to the spooler prior to an unexpected host termination or restart.
    /// Actual physical output is uncertain; must not be automatically re-executed as an original print.
    /// </summary>
    Ambiguous = 6,

    /// <summary>
    /// Job is actively being dispatched to the OS spooler/hardware adapter.
    /// Persisted prior to adapter invocation so a crash mid-call recovers as Ambiguous rather than silently re-printing.
    /// </summary>
    InFlight = 7,

    /// <summary>
    /// Job was intercepted by the peripheral circuit breaker or pre-flight hardware check
    /// and persisted to the durable quarantine store for automatic or manual replay once the printer recovers.
    /// </summary>
    Quarantined = 8
}
