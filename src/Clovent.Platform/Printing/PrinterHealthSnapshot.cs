namespace Clovent.Platform.Printing;

/// <summary>
/// Immutable point-in-time diagnostic snapshot of a printer's physical hardware connectivity,
/// spooler queue depth, and operational fault conditions.
/// </summary>
public sealed record PrinterHealthSnapshot(
    string PrinterName,
    Guid? ProfileId,
    bool IsAvailable,
    PrinterHardwareCondition Condition,
    int SpoolerQueueDepth,
    string? StatusMessage,
    DateTimeOffset CheckedAtUtc)
{
    /// <summary>Whether the printer is online, available, and free of physical fault conditions.</summary>
    public bool IsReady => IsAvailable && Condition == PrinterHardwareCondition.Normal;

    /// <summary>Whether the printer currently has an active physical or communication fault.</summary>
    public bool HasFault => !IsAvailable || Condition != PrinterHardwareCondition.Normal;

    /// <summary>Creates a healthy operational snapshot.</summary>
    public static PrinterHealthSnapshot Healthy(string printerName, Guid? profileId = null, int spoolerQueueDepth = 0) =>
        new(printerName, profileId, true, PrinterHardwareCondition.Normal, spoolerQueueDepth, "Printer hardware is ready.", DateTimeOffset.UtcNow);

    /// <summary>Creates a faulted hardware snapshot with specific condition flags and diagnostic description.</summary>
    public static PrinterHealthSnapshot Faulted(
        string printerName,
        PrinterHardwareCondition condition,
        string statusMessage,
        Guid? profileId = null,
        int spoolerQueueDepth = 0) =>
        new(printerName, profileId, false, condition, spoolerQueueDepth, statusMessage, DateTimeOffset.UtcNow);
}
