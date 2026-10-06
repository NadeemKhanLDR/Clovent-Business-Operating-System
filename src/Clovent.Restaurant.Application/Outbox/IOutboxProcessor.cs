namespace Clovent.Restaurant.Application.Outbox;

/// <summary>Contract for the background outbox processing engine.</summary>
public interface IOutboxProcessor
{
    /// <summary>Whether the background processor loop is currently active.</summary>
    bool IsRunning { get; }

    /// <summary>Executes a single processing pass over claimed pending outbox messages.</summary>
    Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default);

    /// <summary>Signals the processor to wake up and process immediately without waiting for the next polling interval.</summary>
    void TriggerImmediate();

    /// <summary>Recovers messages stuck in Processing state due to unexpected process termination or worker crash.</summary>
    Task<int> RecoverStaleProcessingMessagesAsync(TimeSpan? staleThreshold = null, CancellationToken cancellationToken = default);

    /// <summary>Starts the continuous background processing loop.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the background processing loop gracefully.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
