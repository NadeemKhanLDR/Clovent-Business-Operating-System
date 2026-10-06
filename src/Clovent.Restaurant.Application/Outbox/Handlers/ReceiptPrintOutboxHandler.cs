using System.Text.Json;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Printing;
using Clovent.Restaurant.Outbox;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>
/// Asynchronously prints customer receipts in the background without blocking the cashier.
/// If the printer is offline or out of paper, the sale remains completed and the receipt
/// is marked as PrintPending.
/// </summary>
public sealed class ReceiptPrintOutboxHandler(
    IReceiptPrintService receiptPrintService,
    ICircuitBreakerRegistry circuitBreakerRegistry,
    ILogger<ReceiptPrintOutboxHandler> logger) : IOutboxMessageHandler
{
    private readonly ICircuitBreaker _circuitBreaker = circuitBreakerRegistry.GetOrCreate("ReceiptPrinter");

    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.ReceiptPrint;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<ReceiptPrintPayload>(message.Payload)
            ?? throw new InvalidOperationException($"Invalid payload for ReceiptPrint message {message.Id}.");

        var result = await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await receiptPrintService.PrintReceiptAsync(payload, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        if (result.Status != ReceiptPrintStatus.Printed)
        {
            throw new InvalidOperationException($"Receipt print failed: {result.ErrorMessage}");
        }

        logger.LogInformation("Receipt successfully printed for order {OrderNumber} on printer {Printer}.",
            payload.OrderNumber, result.PrinterName);
    }
}
