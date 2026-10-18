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
        var jobId = payload.OutboxMessageId ?? payload.OrderId;

        // 1. If durable snapshot is available in payload, route and format via dynamic template
        if (!string.IsNullOrWhiteSpace(payload.SnapshotJson))
        {
            ReceiptSnapshot? snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<ReceiptSnapshot>(payload.SnapshotJson);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Receipt snapshot for order {OrderNumber} is corrupted. Refusing fallback text print.", payload.OrderNumber);
                return new ReceiptPrintResult(ReceiptPrintStatus.PrintFailed, null, $"Receipt snapshot for order {payload.OrderNumber} is corrupted: {ex.Message}. Fallback printing rejected.");
            }

            if (snapshot == null)
            {
                _logger?.LogError("Receipt snapshot for order {OrderNumber} is empty or null after deserialization. Refusing fallback text print.", payload.OrderNumber);
                return new ReceiptPrintResult(ReceiptPrintStatus.PrintFailed, null, $"Receipt snapshot for order {payload.OrderNumber} is invalid. Fallback printing rejected.");
            }

            if (!snapshot.IsValid(out var validationError))
            {
                _logger?.LogError("Receipt snapshot for order {OrderNumber} is structurally invalid: {Error}. Refusing fallback text print.", payload.OrderNumber, validationError);
                return new ReceiptPrintResult(ReceiptPrintStatus.PrintFailed, null, $"Receipt snapshot for order {payload.OrderNumber} is structurally invalid: {validationError}. Fallback printing rejected.");
            }

            // Sale ownership from immutable snapshot takes precedence over arbitrary caller-supplied scopes
            var effectiveOrgId = snapshot.OrganizationId ?? payload.OrganizationId;
            var effectiveBranchId = snapshot.BranchId ?? payload.BranchId;
            var effectiveTerminalId = snapshot.TerminalId ?? payload.TerminalId;

            var printableData = snapshot.ToPrintable();
            var terminalSettings = await _printerManagementService.GetTerminalReceiptSettingsAsync(effectiveTerminalId, cancellationToken).ConfigureAwait(false);

            // Validate copy count limits: do not silently truncate or clamp if existing configuration exceeds MaxCopyCount
            if (!terminalSettings.IsCopyCountValid(out var copyValidationError))
            {
                _logger?.LogError("Terminal receipt settings for terminal {TerminalId} are invalid: {Error}", effectiveTerminalId, copyValidationError);
                return new ReceiptPrintResult(
                    ReceiptPrintStatus.PrintFailed,
                    null,
                    $"Print failed: {copyValidationError}");
            }

            PrintDispatchResult? customerResult = null;
            PrintDispatchResult? merchantResult = null;

            // 1. Customer Copy dispatch
            if (terminalSettings.CustomerCopyEnabled)
            {
                var copies = terminalSettings.CustomerCopyCount;
                for (var i = 1; i <= copies; i++)
                {
                    var isAdditionalCopy = i > 1;
                    var copyJobId = i == 1 ? jobId : DeriveSubJobId(jobId, "customer", i);
                    var result = await _printerManagementService.DispatchReceiptAsync(
                        printableData,
                        organizationId: effectiveOrgId,
                        branchId: effectiveBranchId,
                        terminalId: effectiveTerminalId,
                        isReprint: false,
                        reprintCount: 1,
                        reprintReason: null,
                        jobId: copyJobId,
                        copyType: ReceiptCopyType.Customer,
                        customSettings: terminalSettings,
                        targetProfileId: terminalSettings.CustomerCopyProfileId,
                        isAdditionalCopy: isAdditionalCopy,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    customerResult = (customerResult == null || customerResult.Success) ? result : customerResult;
                }
            }

            // 2. Merchant / Store Copy dispatch
            if (terminalSettings.MerchantCopyEnabled)
            {
                var copies = terminalSettings.MerchantCopyCount;
                for (var i = 1; i <= copies; i++)
                {
                    var copyJobId = DeriveSubJobId(jobId, "merchant", i);
                    var result = await _printerManagementService.DispatchReceiptAsync(
                        printableData,
                        organizationId: effectiveOrgId,
                        branchId: effectiveBranchId,
                        terminalId: effectiveTerminalId,
                        isReprint: false,
                        reprintCount: 1,
                        reprintReason: null,
                        jobId: copyJobId,
                        copyType: ReceiptCopyType.Merchant,
                        customSettings: terminalSettings,
                        targetProfileId: terminalSettings.MerchantCopyProfileId,
                        isAdditionalCopy: false,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    merchantResult = (merchantResult == null || merchantResult.Success) ? result : merchantResult;
                }
            }

            if (customerResult == null && merchantResult == null)
            {
                return new ReceiptPrintResult(ReceiptPrintStatus.Printed, "No copies configured", null);
            }

            // Partial multi-copy failure handling: if any copy failed or was quarantined, report that failure
            if (customerResult != null && !customerResult.Success)
            {
                return MapResult(customerResult);
            }

            if (merchantResult != null && !merchantResult.Success)
            {
                return MapResult(merchantResult);
            }

            dispatchResult = customerResult ?? merchantResult!;
            return MapResult(dispatchResult);
        }

        // 2. Missing legacy snapshot: only genuine legacy orders without snapshot JSON fall back to pre-formatted text payload
        if (string.IsNullOrWhiteSpace(payload.ReceiptText))
        {
            _logger?.LogError("Legacy receipt payload for order {OrderNumber} has empty pre-formatted text.", payload.OrderNumber);
            return new ReceiptPrintResult(ReceiptPrintStatus.PrintFailed, null, $"Legacy receipt for order {payload.OrderNumber} has empty receipt text. Cannot print.");
        }

        dispatchResult = await _printerManagementService.DispatchTextAsync(
            payload.ReceiptText,
            documentType: "CustomerReceipt",
            documentReference: payload.OrderNumber,
            organizationId: payload.OrganizationId,
            branchId: payload.BranchId,
            terminalId: payload.TerminalId,
            isReprint: false,
            reprintCount: 1,
            reprintReason: null,
            jobId: jobId,
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

    private static Guid DeriveSubJobId(Guid baseId, string copyType, int index)
    {
        var input = $"{baseId:N}:{copyType}:{index}";
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}

