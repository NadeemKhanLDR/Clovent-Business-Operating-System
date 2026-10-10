using System.ComponentModel;
using System.Drawing.Printing;
using Clovent.Platform.Printing;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Dispatches formatted print jobs to the Windows Print Spooler using GDI+
/// and <see cref="EnhancedReceiptPrintDocument"/>, with proactive hardware health
/// and condition polling prior to job submission.
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
    public Task<PrinterHealthSnapshot> CheckHealthAsync(PrinterProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var queueName = profile.SystemPrinterName;
        if (string.IsNullOrWhiteSpace(queueName))
        {
            return Task.FromResult(PrinterHealthSnapshot.Faulted(
                profile.ProfileName,
                PrinterHardwareCondition.Offline,
                "No Windows printer queue specified for profile.",
                profile.Id));
        }

        var status = _queueProvider.GetPrinterStatus(queueName);

        if (!status.IsInstalled)
        {
            _logger?.LogWarning("Target printer queue '{QueueName}' is not installed.", queueName);
            return Task.FromResult(PrinterHealthSnapshot.Faulted(
                queueName,
                PrinterHardwareCondition.Offline,
                $"Printer queue '{queueName}' is not installed on this system.",
                profile.Id,
                status.QueueJobCount));
        }

        if (status.Condition != PrinterHardwareCondition.Normal)
        {
            _logger?.LogWarning("Printer '{QueueName}' reported hardware condition: {Condition} ({Description}).",
                queueName, status.Condition, status.StatusDescription);
            return Task.FromResult(PrinterHealthSnapshot.Faulted(
                queueName,
                status.Condition,
                $"Printer hardware fault: {status.StatusDescription}",
                profile.Id,
                status.QueueJobCount));
        }

        return Task.FromResult(PrinterHealthSnapshot.Healthy(
            queueName,
            profile.Id,
            status.QueueJobCount));
    }

    /// <inheritdoc/>
    public async Task<PrintDispatchResult> DispatchAsync(
        PrinterProfile profile,
        PrinterJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(job);

        var queueName = profile.SystemPrinterName;
        if (string.IsNullOrWhiteSpace(queueName))
        {
            return PrintDispatchResult.Failed(null, "No Windows printer queue specified for profile.");
        }

        // Proactive hardware pre-flight check
        var health = await CheckHealthAsync(profile, cancellationToken).ConfigureAwait(false);
        if (health.HasFault)
        {
            _logger?.LogWarning("Pre-flight check failed for printer '{QueueName}': {Condition} - {Status}",
                queueName, health.Condition, health.StatusMessage);
            return PrintDispatchResult.Failed(queueName, $"Printer hardware check failed: {health.StatusMessage}");
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
            return PrintDispatchResult.SpoolerAccepted(queueName);
        }
        catch (InvalidPrinterException ex)
        {
            _logger?.LogError(ex, "Printer queue '{QueueName}' is invalid or currently offline.", queueName);
            return PrintDispatchResult.Failed(queueName, $"Printer queue '{queueName}' is invalid or offline: {ex.Message}");
        }
        catch (Win32Exception ex)
        {
            _logger?.LogError(ex, "Win32 spooler error while printing to '{QueueName}'.", queueName);
            return PrintDispatchResult.Failed(queueName, $"Windows spooler error on '{queueName}': {ex.Message} (Error code: {ex.NativeErrorCode})");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error dispatching print job to '{QueueName}'.", queueName);
            return PrintDispatchResult.Failed(queueName, $"Failed to print: {ex.Message}");
        }
    }
}
