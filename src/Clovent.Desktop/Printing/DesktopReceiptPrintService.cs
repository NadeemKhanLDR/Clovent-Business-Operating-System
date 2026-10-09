using System.Text.Json;
using Clovent.Platform.Printing;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Printing;
using Clovent.Restaurant.Orders;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Production desktop implementation of <see cref="IReceiptPrintService"/> that bridges
/// background Transactional Outbox receipt printing to physical or spooler hardware via <see cref="PrinterManagementService"/>.
/// Defines the single owner of automatic receipt dispatch upon order completion.
/// </summary>
public sealed class DesktopReceiptPrintService : IReceiptPrintService
{
    private readonly PrinterManagementService _printerManagementService;
    private readonly ILogger<DesktopReceiptPrintService>? _logger;
    private volatile bool _isSimulatingOutage;

    /// <summary>Initializes a new instance of <see cref="DesktopReceiptPrintService"/>.</summary>
    public DesktopReceiptPrintService(
        PrinterManagementService printerManagementService,
        ILogger<DesktopReceiptPrintService>? logger = null)
    {
        _printerManagementService = printerManagementService ?? throw new ArgumentNullException(nameof(printerManagementService));
        _logger = logger;
    }

    /// <inheritdoc/>
    public void SetSimulatedOutage(bool isOutage) => _isSimulatingOutage = isOutage;

    /// <inheritdoc/>
    public async Task<ReceiptPrintResult> PrintReceiptAsync(ReceiptPrintPayload payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (_isSimulatingOutage)
        {
            throw new InvalidOperationException("POS Receipt Printer is offline or communication timed out (Paper out / Port unreachable).");
        }

        PrintDispatchResult dispatchResult;

        // 1. If durable snapshot is available in payload, route and format via dynamic template
        if (!string.IsNullOrWhiteSpace(payload.SnapshotJson))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<ReceiptSnapshot>(payload.SnapshotJson);
                if (snapshot != null)
                {
                    var printableData = snapshot.ToPrintable();
                    dispatchResult = await _printerManagementService.DispatchReceiptAsync(
                        printableData,
                        organizationId: null,
                        branchId: null,
                        terminalId: null,
                        isReprint: false,
                        reprintCount: 1,
                        reprintReason: null,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    return MapResult(dispatchResult);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to parse durable snapshot for order {OrderNumber}. Falling back to payload text.", payload.OrderNumber);
            }
        }

        // 2. Fallback to pre-formatted text payload
        dispatchResult = await _printerManagementService.DispatchTextAsync(
            payload.ReceiptText,
            documentType: "CustomerReceipt",
            documentReference: payload.OrderNumber,
            organizationId: null,
            branchId: null,
            terminalId: null,
            isReprint: false,
            reprintCount: 1,
            reprintReason: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return MapResult(dispatchResult);
    }

    private static ReceiptPrintResult MapResult(PrintDispatchResult result)
    {
        if (result.Success)
        {
            return new ReceiptPrintResult(ReceiptPrintStatus.Printed, result.TargetQueueName ?? "Default POS Printer", null);
        }

        return new ReceiptPrintResult(ReceiptPrintStatus.PrintFailed, result.TargetQueueName, result.ErrorMessage);
    }
}
