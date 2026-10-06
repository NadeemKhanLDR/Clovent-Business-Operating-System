using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Infrastructure.Outbox;

/// <summary>
/// Resilient background worker processing transactional outbox messages.
/// Features atomic claiming, bounded concurrency, exponential backoff with jitter,
/// dead-letter routing, and crash recovery for interrupted jobs.
/// </summary>
public sealed class OutboxProcessor : IOutboxProcessor, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly CancellationTokenSource _cts = new();

    private Task? _processingLoopTask;
    private volatile bool _isRunning;
    private readonly int _maxConcurrency = 4;
    private readonly int _batchSize = 20;
    private readonly int _maxRetries = 5;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(1);

    /// <summary>Initializes a new instance of <see cref="OutboxProcessor"/>.</summary>
    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsRunning => _isRunning;

    /// <inheritdoc/>
    public void TriggerImmediate()
    {
        if (_signal.CurrentCount == 0)
        {
            try { _signal.Release(); } catch (SemaphoreFullException) { }
        }
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return Task.CompletedTask;

        _isRunning = true;
        _processingLoopTask = Task.Run(() => ProcessingLoopAsync(_cts.Token), CancellationToken.None);
        _logger.LogInformation("Transactional Outbox processing engine started.");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;

        _isRunning = false;
        _cts.Cancel();
        TriggerImmediate();

        if (_processingLoopTask != null)
        {
            try
            {
                await _processingLoopTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Outbox processing loop stopped with warning.");
            }
        }

        _logger.LogInformation("Transactional Outbox processing engine stopped.");
    }

    /// <inheritdoc/>
    public async Task<int> RecoverStaleProcessingMessagesAsync(TimeSpan? staleThreshold = null, CancellationToken cancellationToken = default)
    {
        var threshold = staleThreshold ?? TimeSpan.FromMinutes(2);
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var stale = await repository.GetStaleProcessingMessagesAsync(threshold, cancellationToken).ConfigureAwait(false);
        if (stale.Count == 0) return 0;

        _logger.LogWarning("Found {Count} stale processing outbox messages left behind by crash/termination. Resetting for recovery.", stale.Count);
        foreach (var message in stale)
        {
            message.ResetForRecovery();
            await repository.UpdateAsync(message, cancellationToken).ConfigureAwait(false);
        }

        return stale.Count;
    }

    /// <inheritdoc/>
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var handlers = scope.ServiceProvider.GetServices<IOutboxMessageHandler>().ToDictionary(h => h.MessageType, h => h, StringComparer.OrdinalIgnoreCase);

        var messages = await repository.ClaimMessagesAsync(_batchSize, cancellationToken).ConfigureAwait(false);
        if (messages.Count == 0) return 0;

        using var concurrencyLimiter = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        var tasks = new List<Task>();

        foreach (var message in messages)
        {
            await concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ProcessSingleMessageAsync(message, handlers, repository, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    concurrencyLimiter.Release();
                }
            }, CancellationToken.None));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return messages.Count;
    }

    private async Task ProcessSingleMessageAsync(
        OutboxMessage message,
        Dictionary<string, IOutboxMessageHandler> handlers,
        IOutboxRepository repository,
        CancellationToken cancellationToken)
    {
        if (!handlers.TryGetValue(message.MessageType, out var handler))
        {
            _logger.LogError("No outbox handler registered for message type '{MessageType}'. Routing message {Id} to DeadLetter.", message.MessageType, message.Id);
            message.MarkDeadLetter($"No handler registered for message type '{message.MessageType}'.");
            await repository.UpdateAsync(message, CancellationToken.None).ConfigureAwait(false);
            return;
        }

        try
        {
            await handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
            message.MarkCompleted(DateTimeOffset.UtcNow);
            await repository.UpdateAsync(message, CancellationToken.None).ConfigureAwait(false);
            _logger.LogDebug("Completed outbox message {Id} of type {MessageType}.", message.Id, message.MessageType);
        }
        catch (CircuitBreakerOpenException cbEx)
        {
            var cooldown = cbEx.CooldownRemaining ?? TimeSpan.FromSeconds(30);
            var nextRetry = DateTimeOffset.UtcNow + cooldown;
            _logger.LogWarning("Circuit breaker open for outbox message {Id} ({MessageType}). Rescheduling retry at {NextRetry}.",
                message.Id, message.MessageType, nextRetry);

            message.ScheduleRetry(nextRetry, cbEx.Message);
            await repository.UpdateAsync(message, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Attempt {Attempt} failed for outbox message {Id} ({MessageType}).",
                message.AttemptCount, message.Id, message.MessageType);

            if (message.AttemptCount >= _maxRetries)
            {
                _logger.LogError(ex, "Max retries ({Max}) exceeded for outbox message {Id} ({MessageType}). Moving to DeadLetter queue.",
                    _maxRetries, message.Id, message.MessageType);
                message.MarkDeadLetter(ex.Message);
            }
            else
            {
                // Exponential backoff with jitter: BaseSeconds * 2^(attempt-1) + jitter
                var baseDelaySeconds = Math.Pow(2, Math.Min(message.AttemptCount - 1, 6)) * 2; // 2s, 4s, 8s, 16s...
                var jitterMs = Random.Shared.Next(-500, 500);
                var totalDelay = TimeSpan.FromSeconds(baseDelaySeconds) + TimeSpan.FromMilliseconds(jitterMs);
                if (totalDelay < TimeSpan.FromSeconds(1)) totalDelay = TimeSpan.FromSeconds(1);

                var nextRetry = DateTimeOffset.UtcNow + totalDelay;
                message.ScheduleRetry(nextRetry, ex.Message);
            }

            await repository.UpdateAsync(message, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task ProcessingLoopAsync(CancellationToken cancellationToken)
    {
        // On initial startup, recover any stale processing messages from previous process crashes
        try
        {
            await RecoverStaleProcessingMessagesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute startup crash recovery for stale outbox messages.");
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var processedCount = await ProcessBatchAsync(cancellationToken).ConfigureAwait(false);
                if (processedCount == 0)
                {
                    // Wait for signal or timeout
                    await _signal.WaitAsync(_pollingInterval, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in OutboxProcessor background loop. Sleeping before retry.");
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _signal.Dispose();
    }
}
