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
}
