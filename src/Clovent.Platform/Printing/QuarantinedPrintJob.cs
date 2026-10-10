namespace Clovent.Platform.Printing;

/// <summary>
/// Durable persisted entity representing a failed or diverted receipt print job
/// buffered in quarantined disk storage pending peripheral hardware recovery.
/// </summary>
public sealed class QuarantinedPrintJob
{
    /// <summary>Unique identifier of the quarantined print job.</summary>
    public Guid JobId { get; init; } = Guid.NewGuid();

    /// <summary>Distributed tracing or POS session correlation identifier.</summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    /// <summary>Type of document (e.g., "CustomerReceipt", "ReprintReceipt", "KitchenTicket").</summary>
    public string DocumentType { get; init; } = "CustomerReceipt";

    /// <summary>The target printer profile identifier.</summary>
    public Guid TargetPrinterProfileId { get; init; }

    /// <summary>The Windows printer queue name targeted at dispatch time.</summary>
    public string TargetSystemPrinterName { get; init; } = string.Empty;

    /// <summary>The formatted plain-text or command payload preserved for reproduction.</summary>
    public string PayloadText { get; init; } = string.Empty;

    /// <summary>Actionable physical diagnostic message explaining why dispatch failed and quarantined.</summary>
    public string FailureReason { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the job was written to the quarantine buffer.</summary>
    public DateTimeOffset QuarantinedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Total recovery re-dispatch attempts executed against this job.</summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>UTC timestamp of the most recent re-dispatch recovery attempt.</summary>
    public DateTimeOffset? LastRetryAtUtc { get; set; }

    /// <summary>Diagnostic error message from the most recent recovery attempt, if any.</summary>
    public string? LastRetryError { get; set; }

    /// <summary>Organization scope identifier.</summary>
    public Guid? OrganizationId { get; init; }

    /// <summary>Branch scope identifier.</summary>
    public Guid? BranchId { get; init; }

    /// <summary>Terminal workstation identifier.</summary>
    public Guid? TerminalId { get; init; }

    /// <summary>Whether this document is a reprint.</summary>
    public bool IsReprint { get; init; }

    /// <summary>Sequential reprint count.</summary>
    public int ReprintCount { get; init; } = 1;

    /// <summary>Documented reprint justification.</summary>
    public string? ReprintReason { get; init; }
}
