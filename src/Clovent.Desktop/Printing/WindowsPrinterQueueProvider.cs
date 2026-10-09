using System.Drawing.Printing;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Standard Windows driver/spooler implementation of <see cref="IWindowsPrinterQueueProvider"/>.
/// Queries installed printers via <see cref="PrinterSettings.InstalledPrinters"/>.
/// </summary>
public sealed class WindowsPrinterQueueProvider : IWindowsPrinterQueueProvider
{
    /// <inheritdoc/>
    public IReadOnlyList<WindowsPrinterInfo> GetInstalledPrinters()
    {
        var list = new List<WindowsPrinterInfo>();
        var defaultPrinter = GetDefaultPrinterName();

        try
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                var isDefault = string.Equals(printer, defaultPrinter, StringComparison.OrdinalIgnoreCase);
                var isNetwork = printer.StartsWith(@"\\", StringComparison.Ordinal);
                list.Add(new WindowsPrinterInfo(printer, isDefault, isNetwork, "Ready"));
            }
        }
        catch
        {
            // Fallback for restricted execution environments
        }

        return list;
    }

    /// <inheritdoc/>
    public bool IsPrinterInstalled(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return false;
        }

        try
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (string.Equals(printer, printerName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignored
        }

        return false;
    }

    /// <inheritdoc/>
    public string? GetDefaultPrinterName()
    {
        try
        {
            var settings = new PrinterSettings();
            return settings.PrinterName;
        }
        catch
        {
            return null;
        }
    }
}
