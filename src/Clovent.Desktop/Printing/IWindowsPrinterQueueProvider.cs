using Clovent.Platform.Printing;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Information regarding an installed Windows print queue.
/// </summary>
public sealed record WindowsPrinterInfo(
    string Name,
    bool IsDefault,
    bool IsNetwork,
    string Status);

/// <summary>
/// Detailed physical hardware connectivity, condition flags, and spooler depth of a printer queue.
/// </summary>
public sealed record PrinterHardwareStatusInfo(
    string PrinterName,
    bool IsInstalled,
    bool IsOnline,
    PrinterHardwareCondition Condition,
    int QueueJobCount,
    string StatusDescription);

/// <summary>
/// Provides discovery and status query capabilities for installed Windows print queues.
/// </summary>
public interface IWindowsPrinterQueueProvider
{
    /// <summary>Lists all print queues currently installed on the host operating system.</summary>
    IReadOnlyList<WindowsPrinterInfo> GetInstalledPrinters();

    /// <summary>Checks whether a specific named printer queue exists on the host.</summary>
    bool IsPrinterInstalled(string printerName);

    /// <summary>Returns the name of the system default printer queue, if any.</summary>
    string? GetDefaultPrinterName();

    /// <summary>Queries physical hardware status, condition flags, and queue depth for a named printer.</summary>
    PrinterHardwareStatusInfo GetPrinterStatus(string printerName);
}
