using System.ComponentModel;
using System.Drawing.Printing;
using Clovent.Platform.Printing;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Dispatches formatted print jobs to the Windows Print Spooler using GDI+
/// and <see cref="EnhancedReceiptPrintDocument"/>.
/// </summary>
public sealed class WindowsSpoolerPrinterAdapter : IPrinterAdapter
{
    private readonly IWindowsPrinterQueueProvider _queueProvider;
    private readonly ILogger<WindowsSpoolerPrinterAdapter>? _logger;

    /// <inheritdoc/>
    public PrinterConnectionType SupportedConnection => PrinterConnectionType.WindowsDriver;

    /// <summary>Constructs a Windows spooler printer adapter.</summary>
    public WindowsSpoolerPrinterAdapter(
        IWindowsPrinterQueueProvider queueProvider,
        ILogger<WindowsSpoolerPrinterAdapter>? logger = null)
    {
        _queueProvider = queueProvider ?? throw new ArgumentNullException(nameof(queueProvider));
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<bool> IsAvailableAsync(PrinterProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(profile.SystemPrinterName))
        {
            return Task.FromResult(false);
        }

        var exists = _queueProvider.IsPrinterInstalled(profile.SystemPrinterName);
        return Task.FromResult(exists);
    }

    /// <inheritdoc/>
    public Task<PrintDispatchResult> DispatchAsync(
        PrinterProfile profile,
        PrinterJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(job);

        var queueName = profile.SystemPrinterName;
        if (string.IsNullOrWhiteSpace(queueName))
        {
            return Task.FromResult(PrintDispatchResult.Failed(null, "No Windows printer queue specified for profile."));
        }

        if (!_queueProvider.IsPrinterInstalled(queueName))
        {
            _logger?.LogWarning("Target printer queue '{QueueName}' is not installed on this system.", queueName);
            return Task.FromResult(PrintDispatchResult.Failed(queueName, $"Target Windows printer queue '{queueName}' is not installed on this system."));
        }

        try
        {
            var containsUrdu = ReceiptSnapshotFormatter.HasComplexScript(job.PayloadText);
            using var doc = new EnhancedReceiptPrintDocument(job.PayloadText, profile.PaperWidth, containsUrdu);

            doc.PrinterSettings.PrinterName = queueName;
            doc.PrinterSettings.Copies = (short)Math.Clamp(profile.PrintCopies, 1, 10);

            // Execute print dispatch to spooler
            doc.Print();

            _logger?.LogInformation("Successfully submitted print job {JobId} to Windows queue '{QueueName}'.", job.JobId, queueName);
            return Task.FromResult(PrintDispatchResult.SpoolerAccepted(queueName));
        }
        catch (InvalidPrinterException ex)
        {
            _logger?.LogError(ex, "Printer queue '{QueueName}' is invalid or currently offline.", queueName);
            return Task.FromResult(PrintDispatchResult.Failed(queueName, $"Printer queue '{queueName}' is invalid or offline: {ex.Message}"));
        }
        catch (Win32Exception ex)
        {
            _logger?.LogError(ex, "Win32 spooler error while printing to '{QueueName}'.", queueName);
            return Task.FromResult(PrintDispatchResult.Failed(queueName, $"Windows spooler error on '{queueName}': {ex.Message} (Error code: {ex.NativeErrorCode})"));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error dispatching print job to '{QueueName}'.", queueName);
            return Task.FromResult(PrintDispatchResult.Failed(queueName, $"Failed to print: {ex.Message}"));
        }
    }
}
