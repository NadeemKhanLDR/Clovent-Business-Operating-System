namespace Clovent.Platform.Printing;

/// <summary>
/// Represents the outcome of dispatching a print job to a printer adapter.
/// Distinguishes between spooler acceptance, confirmed completion, and actionable failure.
/// </summary>
public sealed record PrintDispatchResult(
    bool Success,
    PrinterJobStatus Status,
    string? TargetQueueName,
    string? ErrorMessage = null,
    bool IsQuarantined = false)
{
    /// <summary>Creates a result indicating the job was accepted by the Windows print spooler.</summary>
    public static PrintDispatchResult SpoolerAccepted(string queueName) =>
        new(true, PrinterJobStatus.Submitted, queueName);

    /// <summary>Creates a result indicating the job was confirmed completed by the device/spooler.</summary>
    public static PrintDispatchResult Confirmed(string queueName) =>
        new(true, PrinterJobStatus.Confirmed, queueName);

    /// <summary>Creates a result indicating the print dispatch failed.</summary>
    public static PrintDispatchResult Failed(string? queueName, string errorMessage) =>
        new(false, PrinterJobStatus.Failed, queueName, errorMessage);

    /// <summary>Creates a result indicating the print dispatch is in an ambiguous or already-in-progress state and must not be duplicated.</summary>
    public static PrintDispatchResult Ambiguous(string? queueName, string errorMessage) =>
        new(false, PrinterJobStatus.Ambiguous, queueName, errorMessage);

    /// <summary>Creates a result indicating the print job was intercepted and safely quarantined to durable disk storage.</summary>
    public static PrintDispatchResult Quarantined(string? queueName, string errorMessage) =>
        new(false, PrinterJobStatus.Quarantined, queueName, errorMessage, IsQuarantined: true);
}
