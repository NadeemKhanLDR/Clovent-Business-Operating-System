namespace Clovent.Platform.Printing;

/// <summary>
/// Represents a trackable print job dispatched to a hardware queue or adapter.
/// Preserves job identity, target profile, reprint history, and audit metadata.
/// </summary>
public sealed class PrinterJob
{
    /// <summary>Unique identifier for this print job.</summary>
    public Guid JobId { get; init; } = Guid.NewGuid();

    /// <summary>Correlation identifier for cross-layer distributed tracing.</summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    /// <summary>Organization scope this job belongs to.</summary>
    public Guid? OrganizationId { get; init; }

    /// <summary>Branch scope this job originated from.</summary>
    public Guid? BranchId { get; init; }

    /// <summary>Terminal scope this job originated from.</summary>
    public Guid? TerminalId { get; init; }

    /// <summary>Type of document (e.g. "CustomerReceipt", "ReprintReceipt", "KitchenTicket", "TestPrint").</summary>
    public string DocumentType { get; init; } = "CustomerReceipt";

    /// <summary>The target logical printer profile identifier.</summary>
    public Guid TargetPrinterProfileId { get; set; }

    /// <summary>The system printer queue name resolved at submission time.</summary>
    public string TargetSystemPrinterName { get; set; } = string.Empty;

    /// <summary>Current lifecycle status of this job.</summary>
    public PrinterJobStatus Status { get; set; } = PrinterJobStatus.Requested;

    /// <summary>Whether this print job represents a reprint of a completed sale.</summary>
    public bool IsReprint { get; init; }

    /// <summary>The sequential reprint number (1 for original copy, 2+ for reprints).</summary>
    public int ReprintCount { get; init; } = 1;

    /// <summary>The documented reason for the reprint, if applicable.</summary>
    public string? ReprintReason { get; init; }

    /// <summary>The formatted plain-text or command payload to print.</summary>
    public string PayloadText { get; init; } = string.Empty;

    /// <summary>Actionable error message if the job failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>UTC timestamp when the job was requested.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>UTC timestamp when the job was accepted by the print spooler.</summary>
    public DateTimeOffset? SubmittedAtUtc { get; set; }

    /// <summary>UTC timestamp when the job reached terminal status (Confirmed or Failed).</summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>Creates a new print job.</summary>
    public static PrinterJob Create(
        string documentType,
        Guid profileId,
        string systemPrinterName,
        string payloadText,
        Guid? organizationId = null,
        Guid? branchId = null,
        Guid? terminalId = null,
        bool isReprint = false,
        int reprintCount = 1,
        string? reprintReason = null,
        Guid? correlationId = null)
    {
        return new PrinterJob
        {
            JobId = Guid.NewGuid(),
            CorrelationId = correlationId ?? Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            TerminalId = terminalId,
            DocumentType = documentType,
            TargetPrinterProfileId = profileId,
            TargetSystemPrinterName = systemPrinterName,
            Status = PrinterJobStatus.Requested,
            IsReprint = isReprint,
            ReprintCount = reprintCount,
            ReprintReason = reprintReason,
            PayloadText = payloadText,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
