namespace Clovent.Restaurant.Outbox;

/// <summary>Summary counts of outbox messages across lifecycle statuses.</summary>
public sealed record OutboxStatistics(
    int PendingCount,
    int ProcessingCount,
    int CompletedCount,
    int RetryScheduledCount,
    int FailedCount,
    int DeadLetterCount,
    DateTimeOffset? OldestPendingAtUtc,
    IReadOnlyDictionary<string, int> PendingByType);

/// <summary>Repository abstraction for durable outbox persistence.</summary>
public interface IOutboxRepository
{
    /// <summary>Persists a new outbox message.</summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>Persists multiple new outbox messages atomically.</summary>
    Task AddRangeAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a message by its ID.</summary>
    Task<OutboxMessage?> GetByIdAsync(OutboxMessageId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a message by its idempotency key if one exists.</summary>
    Task<OutboxMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Claims a batch of pending or retry-scheduled messages for atomic worker processing.</summary>
    Task<IReadOnlyList<OutboxMessage>> ClaimMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing message within persistence boundary.</summary>
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>Finds messages that have remained in Processing state longer than the threshold (crash recovery).</summary>
    Task<IReadOnlyList<OutboxMessage>> GetStaleProcessingMessagesAsync(TimeSpan staleThreshold, CancellationToken cancellationToken = default);

    /// <summary>Gets aggregate statistics for Operations Health monitoring.</summary>
    Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves failed or dead-letter messages for managerial review and manual retry.</summary>
    Task<IReadOnlyList<OutboxMessage>> GetDeadLetterAndFailedMessagesAsync(int maxCount = 50, CancellationToken cancellationToken = default);

    /// <summary>Retrieves uncompleted messages of a specific type (e.g. for inventory reconciliation).</summary>
    Task<IReadOnlyList<OutboxMessage>> GetUncompletedMessagesByTypeAsync(string messageType, int maxCount = 100, CancellationToken cancellationToken = default);
}
