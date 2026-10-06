using Clovent.Restaurant.Application.Outbox.Dtos;

namespace Clovent.Restaurant.Application.Printing;

/// <summary>Status of an asynchronous receipt print job.</summary>
public enum ReceiptPrintStatus
{
    /// <summary>Receipt has been sent to the printer spooler or printer successfully.</summary>
    Printed = 0,

    /// <summary>Printer is offline or busy; receipt is pending retry.</summary>
    PrintPending = 1,

    /// <summary>Printer failure encountered.</summary>
    PrintFailed = 2
}

/// <summary>Result of a print attempt.</summary>
public sealed record ReceiptPrintResult(
    ReceiptPrintStatus Status,
    string? PrinterName,
    string? ErrorMessage);

/// <summary>Service abstraction for dispatching receipt prints.</summary>
public interface IReceiptPrintService
{
    /// <summary>Dispatches a receipt text to the physical printer or spooler.</summary>
    Task<ReceiptPrintResult> PrintReceiptAsync(ReceiptPrintPayload payload, CancellationToken cancellationToken = default);

    /// <summary>Simulates a printer outage for testing / circuit breaker verification.</summary>
    void SetSimulatedOutage(bool isOutage);
}

/// <summary>Default implementation of <see cref="IReceiptPrintService"/>.</summary>
public sealed class DefaultReceiptPrintService : IReceiptPrintService
{
    private volatile bool _isSimulatingOutage;

    /// <inheritdoc/>
    public void SetSimulatedOutage(bool isOutage) => _isSimulatingOutage = isOutage;

    /// <inheritdoc/>
    public async Task<ReceiptPrintResult> PrintReceiptAsync(ReceiptPrintPayload payload, CancellationToken cancellationToken = default)
    {
        if (_isSimulatingOutage)
        {
            throw new InvalidOperationException("POS Receipt Printer is offline or communication timed out (Paper out / Port unreachable).");
        }

        await Task.Delay(5, cancellationToken).ConfigureAwait(false);
        return new ReceiptPrintResult(ReceiptPrintStatus.Printed, payload.TargetPrinter ?? "Default POS Printer", null);
    }
}
