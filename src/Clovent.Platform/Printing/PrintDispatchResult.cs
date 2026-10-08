namespace Clovent.Platform.Printing;

/// <summary>
/// Result of an adapter print dispatch operation.
/// </summary>
public sealed record PrintDispatchResult(
    bool Success,
    PrinterJobStatus Status,
    string? TargetQueueName,
    string? ErrorMessage,
    bool IsHardwareConfirmed = false)
{
    /// <summary>Creates a successful spooler submission result.</summary>
    public static PrintDispatchResult SpoolerAccepted(string queueName) =>
        new(true, PrinterJobStatus.Submitted, queueName, null, false);

    /// <summary>Creates a confirmed physical print result.</summary>
    public static PrintDispatchResult Confirmed(string queueName) =>
        new(true, PrinterJobStatus.Confirmed, queueName, null, true);

    /// <summary>Creates a failed print dispatch result with actionable error message.</summary>
    public static PrintDispatchResult Failed(string? queueName, string errorMessage) =>
        new(false, PrinterJobStatus.Failed, queueName, errorMessage, false);
}
