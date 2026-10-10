using System.Drawing.Printing;
using System.Runtime.InteropServices;
using Clovent.Platform.Printing;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Standard Windows driver and print spooler implementation of <see cref="IWindowsPrinterQueueProvider"/>.
/// Queries installed printers via <see cref="PrinterSettings.InstalledPrinters"/> and queries
/// low-level spooler and hardware health diagnostics via Win32 Spooler API (winspool.drv).
/// </summary>
public sealed class WindowsPrinterQueueProvider : IWindowsPrinterQueueProvider
{
    private const uint PRINTER_ATTRIBUTE_WORK_OFFLINE = 0x00000400;

    private const uint PRINTER_STATUS_PAUSED = 0x00000001;
    private const uint PRINTER_STATUS_ERROR = 0x00000002;
    private const uint PRINTER_STATUS_PENDING_DELETION = 0x00000004;
    private const uint PRINTER_STATUS_PAPER_JAM = 0x00000008;
    private const uint PRINTER_STATUS_PAPER_OUT = 0x00000010;
    private const uint PRINTER_STATUS_MANUAL_FEED = 0x00000020;
    private const uint PRINTER_STATUS_PAPER_PROBLEM = 0x00000040;
    private const uint PRINTER_STATUS_OFFLINE = 0x00000080;
    private const uint PRINTER_STATUS_IO_ACTIVE = 0x00000100;
    private const uint PRINTER_STATUS_BUSY = 0x00000200;
    private const uint PRINTER_STATUS_PRINTING = 0x00000400;
    private const uint PRINTER_STATUS_OUTPUT_BIN_FULL = 0x00000800;
    private const uint PRINTER_STATUS_NOT_AVAILABLE = 0x00001000;
    private const uint PRINTER_STATUS_WAITING = 0x00002000;
    private const uint PRINTER_STATUS_PROCESSING = 0x00004000;
    private const uint PRINTER_STATUS_INITIALIZING = 0x00008000;
    private const uint PRINTER_STATUS_WARMING_UP = 0x00010000;
    private const uint PRINTER_STATUS_TONER_LOW = 0x00020000;
    private const uint PRINTER_STATUS_NO_TONER = 0x00040000;
    private const uint PRINTER_STATUS_PAGE_PUNT = 0x00080000;
    private const uint PRINTER_STATUS_USER_INTERVENTION = 0x00100000;
    private const uint PRINTER_STATUS_OUT_OF_MEMORY = 0x00200000;
    private const uint PRINTER_STATUS_DOOR_OPEN = 0x00400000;
    private const uint PRINTER_STATUS_SERVER_UNKNOWN = 0x00800000;
    private const uint PRINTER_STATUS_POWER_SAVE = 0x01000000;

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "GetPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool GetPrinter(IntPtr hPrinter, int dwLevel, IntPtr pPrinter, int cbBuf, out int pcbNeeded);

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
                var statusInfo = GetPrinterStatus(printer);
                list.Add(new WindowsPrinterInfo(printer, isDefault, isNetwork, statusInfo.StatusDescription));
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

    /// <inheritdoc/>
    public PrinterHardwareStatusInfo GetPrinterStatus(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return new PrinterHardwareStatusInfo(
                printerName ?? string.Empty,
                IsInstalled: false,
                IsOnline: false,
                PrinterHardwareCondition.Offline,
                QueueJobCount: 0,
                "Printer name cannot be empty.");
        }

        var isInstalled = IsPrinterInstalled(printerName);
        if (!isInstalled)
        {
            return new PrinterHardwareStatusInfo(
                printerName,
                IsInstalled: false,
                IsOnline: false,
                PrinterHardwareCondition.Offline,
                QueueJobCount: 0,
                $"Printer queue '{printerName}' is not installed on this system.");
        }

        if (!OperatingSystem.IsWindows())
        {
            return new PrinterHardwareStatusInfo(
                printerName,
                IsInstalled: true,
                IsOnline: true,
                PrinterHardwareCondition.Normal,
                QueueJobCount: 0,
                "Ready");
        }

        IntPtr hPrinter = IntPtr.Zero;
        try
        {
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                return new PrinterHardwareStatusInfo(
                    printerName,
                    IsInstalled: true,
                    IsOnline: false,
                    PrinterHardwareCondition.Offline,
                    QueueJobCount: 0,
                    $"Unable to open printer queue '{printerName}' (ErrorCode: {Marshal.GetLastWin32Error()}).");
            }

            // Determine buffer size for PRINTER_INFO_2
            GetPrinter(hPrinter, 2, IntPtr.Zero, 0, out int bytesNeeded);
            if (bytesNeeded <= 0)
            {
                return new PrinterHardwareStatusInfo(
                    printerName,
                    IsInstalled: true,
                    IsOnline: true,
                    PrinterHardwareCondition.Normal,
                    QueueJobCount: 0,
                    "Ready");
            }

            var buffer = Marshal.AllocHGlobal(bytesNeeded);
            try
            {
                if (!GetPrinter(hPrinter, 2, buffer, bytesNeeded, out _))
                {
                    return new PrinterHardwareStatusInfo(
                        printerName,
                        IsInstalled: true,
                        IsOnline: false,
                        PrinterHardwareCondition.Offline,
                        QueueJobCount: 0,
                        $"Failed to retrieve spooler details for '{printerName}'.");
                }

                // PRINTER_INFO_2 layout calculation:
                // 13 pointers (8 bytes on 64-bit, 4 bytes on 32-bit) followed by DWORDs:
                // Attributes (+0), Priority (+4), DefaultPriority (+8), StartTime (+12), UntilTime (+16), Status (+20), cJobs (+24)
                int ptrSize = IntPtr.Size;
                int attributesOffset = 13 * ptrSize;
                int statusOffset = attributesOffset + (5 * sizeof(uint));
                int cJobsOffset = statusOffset + sizeof(uint);

                uint attributes = (uint)Marshal.ReadInt32(buffer, attributesOffset);
                uint status = (uint)Marshal.ReadInt32(buffer, statusOffset);
                int queueDepth = Marshal.ReadInt32(buffer, cJobsOffset);
                if (queueDepth < 0) queueDepth = 0;

                var condition = PrinterHardwareCondition.Normal;
                var issues = new List<string>();

                if ((status & PRINTER_STATUS_OFFLINE) != 0 || (attributes & PRINTER_ATTRIBUTE_WORK_OFFLINE) != 0)
                {
                    condition |= PrinterHardwareCondition.Offline;
                    issues.Add("Offline");
                }

                if ((status & PRINTER_STATUS_PAPER_OUT) != 0)
                {
                    condition |= PrinterHardwareCondition.PaperOut;
                    issues.Add("Paper Out");
                }

                if ((status & PRINTER_STATUS_DOOR_OPEN) != 0)
                {
                    condition |= PrinterHardwareCondition.CoverOpen;
                    issues.Add("Cover Open");
                }

                if ((status & PRINTER_STATUS_PAPER_JAM) != 0)
                {
                    condition |= PrinterHardwareCondition.PaperJam;
                    issues.Add("Paper Jam");
                }

                if ((status & PRINTER_STATUS_PAPER_PROBLEM) != 0)
                {
                    condition |= PrinterHardwareCondition.CutterError;
                    issues.Add("Cutter / Paper Problem");
                }

                if ((status & PRINTER_STATUS_PAUSED) != 0)
                {
                    condition |= PrinterHardwareCondition.Paused;
                    issues.Add("Paused");
                }

                if ((status & PRINTER_STATUS_OUTPUT_BIN_FULL) != 0)
                {
                    condition |= PrinterHardwareCondition.OutputBinFull;
                    issues.Add("Output Bin Full");
                }

                if ((status & PRINTER_STATUS_ERROR) != 0)
                {
                    condition |= PrinterHardwareCondition.GeneralError;
                    issues.Add("General Error");
                }

                var isOnline = (condition & PrinterHardwareCondition.Offline) == 0;
                var statusDescription = issues.Count > 0 ? string.Join(", ", issues) : "Ready";

                return new PrinterHardwareStatusInfo(
                    printerName,
                    IsInstalled: true,
                    IsOnline: isOnline,
                    condition,
                    queueDepth,
                    statusDescription);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            return new PrinterHardwareStatusInfo(
                printerName,
                IsInstalled: true,
                IsOnline: false,
                PrinterHardwareCondition.GeneralError,
                QueueJobCount: 0,
                $"Spooler query exception: {ex.Message}");
        }
        finally
        {
            if (hPrinter != IntPtr.Zero)
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
