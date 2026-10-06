using Clovent.Restaurant.Outbox;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeOutboxRepository : IOutboxRepository
{
    private readonly Dictionary<OutboxMessageId, OutboxMessage> _messages = [];

    public IReadOnlyCollection<OutboxMessage> AllMessages => _messages.Values;

    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        foreach (var msg in messages)
        {
            _messages[msg.Id] = msg;
        }
        return Task.CompletedTask;
    }

    public Task<OutboxMessage?> GetByIdAsync(OutboxMessageId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_messages.GetValueOrDefault(id));
    }

    public Task<OutboxMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var msg = _messages.Values.FirstOrDefault(m => m.IdempotencyKey == idempotencyKey);
        return Task.FromResult(msg);
    }

    public Task<IReadOnlyList<OutboxMessage>> ClaimMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var claimed = _messages.Values
            .Where(m => (m.Status == OutboxMessageStatus.Pending || m.Status == OutboxMessageStatus.RetryScheduled) && m.AvailableAtUtc <= now)
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.AvailableAtUtc)
            .Take(batchSize)
            .ToList();

        foreach (var msg in claimed)
        {
            msg.ClaimForProcessing(now);
        }

        return Task.FromResult<IReadOnlyList<OutboxMessage>>(claimed);
    }

    public Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> GetStaleProcessingMessagesAsync(TimeSpan staleThreshold, CancellationToken cancellationToken = default)
    {
        var thresholdTime = DateTimeOffset.UtcNow - staleThreshold;
        var stale = _messages.Values
            .Where(m => m.Status == OutboxMessageStatus.Processing && m.ProcessingStartedAtUtc <= thresholdTime)
            .ToList();
        return Task.FromResult<IReadOnlyList<OutboxMessage>>(stale);
    }

    public Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var pending = _messages.Values.Count(m => m.Status == OutboxMessageStatus.Pending);
        var processing = _messages.Values.Count(m => m.Status == OutboxMessageStatus.Processing);
        var completed = _messages.Values.Count(m => m.Status == OutboxMessageStatus.Completed);
        var retry = _messages.Values.Count(m => m.Status == OutboxMessageStatus.RetryScheduled);
        var failed = _messages.Values.Count(m => m.Status == OutboxMessageStatus.Failed);
        var deadLetter = _messages.Values.Count(m => m.Status == OutboxMessageStatus.DeadLetter);
        var oldest = _messages.Values.Where(m => m.Status == OutboxMessageStatus.Pending).Select(m => (DateTimeOffset?)m.CreatedAtUtc).FirstOrDefault();
        var byType = _messages.Values.Where(m => m.Status == OutboxMessageStatus.Pending).GroupBy(m => m.MessageType).ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(new OutboxStatistics(pending, processing, completed, retry, failed, deadLetter, oldest, byType));
    }

    public Task<IReadOnlyList<OutboxMessage>> GetDeadLetterAndFailedMessagesAsync(int maxCount = 50, CancellationToken cancellationToken = default)
    {
        var failed = _messages.Values
            .Where(m => m.Status is OutboxMessageStatus.DeadLetter or OutboxMessageStatus.Failed)
            .Take(maxCount)
            .ToList();
        return Task.FromResult<IReadOnlyList<OutboxMessage>>(failed);
    }

    public Task<IReadOnlyList<OutboxMessage>> GetUncompletedMessagesByTypeAsync(string messageType, int maxCount = 100, CancellationToken cancellationToken = default)
    {
        var uncompleted = _messages.Values
            .Where(m => m.MessageType == messageType && m.Status != OutboxMessageStatus.Completed)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(maxCount)
            .ToList();
        return Task.FromResult<IReadOnlyList<OutboxMessage>>(uncompleted);
    }
}
