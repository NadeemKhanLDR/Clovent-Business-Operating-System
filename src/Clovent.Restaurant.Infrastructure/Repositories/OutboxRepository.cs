using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IOutboxRepository"/>.</summary>
public sealed class OutboxRepository(RestaurantDbContext dbContext) : IOutboxRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await dbContext.OutboxMessages.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddRangeAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        await dbContext.OutboxMessages.AddRangeAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OutboxMessage?> GetByIdAsync(OutboxMessageId id, CancellationToken cancellationToken = default)
    {
        return await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OutboxMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OutboxMessage>> ClaimMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var pendingStatus = OutboxMessageStatus.Pending;
        var retryStatus = OutboxMessageStatus.RetryScheduled;

        var allEligible = await dbContext.OutboxMessages
            .Where(m => m.Status == pendingStatus || m.Status == retryStatus)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var candidates = allEligible
            .Where(m => m.AvailableAtUtc <= now)
            .OrderByDescending(m => m.Priority)
            .ThenBy(m => m.AvailableAtUtc)
            .Take(batchSize)
            .ToList();

        if (candidates.Count == 0)
        {
            return Array.Empty<OutboxMessage>();
        }

        var claimed = new List<OutboxMessage>();
        foreach (var message in candidates)
        {
            try
            {
                message.MarkProcessing(now);
                claimed.Add(message);
            }
            catch (InvalidOperationException)
            {
                // Concurrently changed status, skip
            }
        }

        if (claimed.Count > 0)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                // If another worker claimed any overlapping message concurrently, return those that were tracked
            }
        }

        return claimed;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        dbContext.OutboxMessages.Update(message);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OutboxMessage>> GetStaleProcessingMessagesAsync(TimeSpan staleThreshold, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - staleThreshold;
        return await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Processing && m.ProcessingStartedAtUtc != null && m.ProcessingStartedAtUtc <= cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var messages = await dbContext.OutboxMessages
            .Select(m => new { m.Status, m.MessageType, m.CreatedAtUtc, m.AvailableAtUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pendingCount = messages.Count(m => m.Status == OutboxMessageStatus.Pending);
        var processingCount = messages.Count(m => m.Status == OutboxMessageStatus.Processing);
        var completedCount = messages.Count(m => m.Status == OutboxMessageStatus.Completed);
        var retryScheduledCount = messages.Count(m => m.Status == OutboxMessageStatus.RetryScheduled);
        var failedCount = messages.Count(m => m.Status == OutboxMessageStatus.Failed);
        var deadLetterCount = messages.Count(m => m.Status == OutboxMessageStatus.DeadLetter);

        var oldestPending = messages
            .Where(m => m.Status == OutboxMessageStatus.Pending || m.Status == OutboxMessageStatus.RetryScheduled)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => (DateTimeOffset?)m.CreatedAtUtc)
            .FirstOrDefault();

        var pendingByType = messages
            .Where(m => m.Status == OutboxMessageStatus.Pending || m.Status == OutboxMessageStatus.RetryScheduled)
            .GroupBy(m => m.MessageType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new OutboxStatistics(
            pendingCount,
            processingCount,
            completedCount,
            retryScheduledCount,
            failedCount,
            deadLetterCount,
            oldestPending,
            pendingByType);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OutboxMessage>> GetDeadLetterAndFailedMessagesAsync(int maxCount = 50, CancellationToken cancellationToken = default)
    {
        return await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.DeadLetter || m.Status == OutboxMessageStatus.Failed)
            .OrderByDescending(m => m.LastAttemptAtUtc ?? m.CreatedAtUtc)
            .Take(maxCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OutboxMessage>> GetUncompletedMessagesByTypeAsync(string messageType, int maxCount = 100, CancellationToken cancellationToken = default)
    {
        var allByType = await dbContext.OutboxMessages
            .Where(m => m.MessageType == messageType)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return allByType
            .Where(m => m.Status != OutboxMessageStatus.Completed)
            .Take(maxCount)
            .ToList()
            .AsReadOnly();
    }
}
