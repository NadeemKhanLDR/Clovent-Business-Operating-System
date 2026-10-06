using System.Text.Json;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Outbox;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Outbox.Handlers;

/// <summary>Processes analytical and activity events in the background.</summary>
public sealed class AnalyticsEventOutboxHandler(
    IMediator mediator,
    ILogger<AnalyticsEventOutboxHandler> logger) : IOutboxMessageHandler
{
    /// <inheritdoc/>
    public string MessageType => OutboxMessageType.AnalyticsEvent;

    /// <inheritdoc/>
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<AnalyticsEventPayload>(message.Payload)
            ?? throw new InvalidOperationException($"Invalid payload for AnalyticsEvent message {message.Id}.");

        try
        {
            await mediator.Send(
                new RecordActivityCommand(payload.EventName, payload.Details, "System/BackgroundWorker", Environment.MachineName),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record background analytics event {EventName}.", payload.EventName);
            // Non-critical: does not fail the transaction
        }
    }
}
