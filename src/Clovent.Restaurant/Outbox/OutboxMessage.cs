using Clovent.Domain;

namespace Clovent.Restaurant.Outbox;

/// <summary>
/// A reliable transactional outbox message recorded atomically with domain state
/// and processed asynchronously by background workers.
/// </summary>
public sealed class OutboxMessage : AggregateRoot<OutboxMessageId>
{
    private OutboxMessage() { }

    /// <summary>The discriminator / routing key for the destination handler.</summary>
    public string MessageType { get; private set; } = string.Empty;

    /// <summary>The name of the aggregate that produced this message (e.g. "Order", "Payment").</summary>
    public string AggregateType { get; private set; } = string.Empty;

    /// <summary>The identifier of the aggregate that produced this message.</summary>
    public string AggregateId { get; private set; } = string.Empty;

    /// <summary>Distributed correlation identifier tracing the end-to-end user operation.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Unique idempotency key preventing duplicate downstream effect.</summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>Serialized JSON payload for the worker.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>Current lifecycle status.</summary>
    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;

    /// <summary>Number of processing attempts executed so far.</summary>
    public int AttemptCount { get; private set; }

    /// <summary>When the message was initially created in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Earliest time the message becomes eligible for polling/processing.</summary>
    public DateTimeOffset AvailableAtUtc { get; private set; }

    /// <summary>Timestamp when current or last processing attempt began.</summary>
    public DateTimeOffset? ProcessingStartedAtUtc { get; private set; }

    /// <summary>Timestamp when the message was successfully completed.</summary>
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>Timestamp when the last attempt occurred.</summary>
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }

    /// <summary>Error message or exception details from the most recent failure.</summary>
    public string? LastError { get; private set; }

    /// <summary>Timestamp when the next retry should take place.</summary>
    public DateTimeOffset? NextRetryAtUtc { get; private set; }

    /// <summary>Priority integer where lower values or higher values can be ordered (default 0).</summary>
    public int Priority { get; private set; }

    /// <summary>Concurrency stamp for optimistic concurrency control.</summary>
    public byte[] Version { get; private set; } = [];

    /// <summary>Creates a new pending outbox message.</summary>
    public static OutboxMessage Create(
        string messageType,
        string aggregateType,
        string aggregateId,
        string correlationId,
        string payload,
        string? idempotencyKey = null,
        int priority = 0,
        DateTimeOffset? availableAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var now = DateTimeOffset.UtcNow;
        return new OutboxMessage
        {
            Id = OutboxMessageId.New(),
            MessageType = messageType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
            Payload = payload,
            Status = OutboxMessageStatus.Pending,
            AttemptCount = 0,
            CreatedAtUtc = now,
            AvailableAtUtc = availableAtUtc ?? now,
            Priority = priority,
            Version = Guid.NewGuid().ToByteArray()
        };
    }

    /// <summary>Marks the message as claimed and currently processing.</summary>
    public void MarkProcessing(DateTimeOffset startedAtUtc)
    {
        if (Status != OutboxMessageStatus.Pending && Status != OutboxMessageStatus.RetryScheduled)
        {
            throw new InvalidOperationException($"Cannot claim message {Id} in status {Status}.");
        }

        Status = OutboxMessageStatus.Processing;
        ProcessingStartedAtUtc = startedAtUtc;
        LastAttemptAtUtc = startedAtUtc;
        AttemptCount++;
        Version = Guid.NewGuid().ToByteArray();
    }

    /// <summary>Claims the message for processing at the current timestamp.</summary>
    public void ClaimForProcessing(DateTimeOffset? startedAtUtc = null)
        => MarkProcessing(startedAtUtc ?? DateTimeOffset.UtcNow);

    /// <summary>Marks the message as successfully completed.</summary>
    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        Status = OutboxMessageStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        LastError = null;
        NextRetryAtUtc = null;
        Version = Guid.NewGuid().ToByteArray();
    }

    /// <summary>Marks the message as successfully completed at current UTC timestamp.</summary>
    public void MarkCompleted() => MarkCompleted(DateTimeOffset.UtcNow);

    /// <summary>Schedules an exponential backoff retry for the message.</summary>
    public void ScheduleRetry(DateTimeOffset nextRetryAtUtc, string errorMessage)
    {
        Status = OutboxMessageStatus.RetryScheduled;
        NextRetryAtUtc = nextRetryAtUtc;
        AvailableAtUtc = nextRetryAtUtc;
        LastError = errorMessage;
        ProcessingStartedAtUtc = null;
        Version = Guid.NewGuid().ToByteArray();
    }

    /// <summary>Defers an outbox message due to offline network or circuit breaker cooldown without consuming retry budget.</summary>
    public void DeferForOffline(DateTimeOffset nextRetryAtUtc, string reason)
    {
        Status = OutboxMessageStatus.RetryScheduled;
        NextRetryAtUtc = nextRetryAtUtc;
        AvailableAtUtc = nextRetryAtUtc;
        LastError = reason;
        ProcessingStartedAtUtc = null;
        if (AttemptCount > 0)
        {
            AttemptCount--; // Reverse the attempt penalty for planned offline periods
        }
        Version = Guid.NewGuid().ToByteArray();
    }

    /// <summary>Schedules a retry with exponential backoff or routes to dead-letter if max attempts are exceeded.</summary>
    public void ScheduleRetry(string errorMessage, int maxAttempts = 5)
    {
        if (AttemptCount >= maxAttempts)
        {
            MarkDeadLetter($"Max retry attempts ({maxAttempts}) exceeded: {errorMessage}");
            return;
        }

        var delaySeconds = Math.Max(1.0, Math.Pow(2, Math.Min(AttemptCount - 1, 6)) * 2);
        var nextRetry = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
        ScheduleRetry(nextRetry, errorMessage);
    }

    /// <summary>Marks the message as permanently moved to the dead-letter queue.</summary>
    public void MarkDeadLetter(string errorMessage)
    {
        Status = OutboxMessageStatus.DeadLetter;
        LastError = errorMessage;
        NextRetryAtUtc = null;
        ProcessingStartedAtUtc = null;
        Version = Guid.NewGuid().ToByteArray();
    }

    /// <summary>Marks the message as failed.</summary>
    public void MarkFailed(string errorMessage)
    {
        Status = OutboxMessageStatus.Failed;
        LastError = errorMessage;
        NextRetryAtUtc = null;
        ProcessingStartedAtUtc = null;
        Version = Guid.NewGuid().ToByteArray();
    }

    /// <summary>Resets a stuck/abandoned processing message back to pending/retryable.</summary>
    public void ResetForRecovery()
    {
        Status = OutboxMessageStatus.Pending;
        AvailableAtUtc = DateTimeOffset.UtcNow;
        ProcessingStartedAtUtc = null;
        Version = Guid.NewGuid().ToByteArray();
    }

    /// <summary>Recovers a stale processing message back to RetryScheduled with diagnostic error.</summary>
    public void RecoverFromStaleProcessing(string reason = "Worker crash recovery")
    {
        Status = OutboxMessageStatus.RetryScheduled;
        LastError = reason;
        AvailableAtUtc = DateTimeOffset.UtcNow;
        ProcessingStartedAtUtc = null;
        Version = Guid.NewGuid().ToByteArray();
    }

    /// <summary>Manually requests an immediate retry (e.g. from Operations Health screen).</summary>
    public void RetryNow()
    {
        Status = OutboxMessageStatus.Pending;
        AvailableAtUtc = DateTimeOffset.UtcNow;
        NextRetryAtUtc = null;
        ProcessingStartedAtUtc = null;
        Version = Guid.NewGuid().ToByteArray();
    }
}
