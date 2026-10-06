using Clovent.Restaurant.Outbox;

namespace Clovent.Restaurant.Application.Outbox;

/// <summary>Contract for handlers that process specific outbox message types asynchronously.</summary>
public interface IOutboxMessageHandler
{
    /// <summary>The message type this handler processes (e.g. <see cref="OutboxMessageType.InventoryPosting"/>).</summary>
    string MessageType { get; }

    /// <summary>Executes the work item represented by the message.</summary>
    Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken);
}
