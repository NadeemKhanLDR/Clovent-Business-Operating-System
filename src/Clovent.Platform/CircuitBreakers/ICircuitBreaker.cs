namespace Clovent.Platform.CircuitBreakers;

/// <summary>Contract for fault isolation and failure prevention around external/non-critical dependencies.</summary>
public interface ICircuitBreaker
{
    /// <summary>Name identifying the protected dependency (e.g., "QuickBooks", "ReceiptPrinter").</summary>
    string Name { get; }

    /// <summary>Current state of the circuit breaker.</summary>
    CircuitBreakerState State { get; }

    /// <summary>Count of consecutive recorded failures.</summary>
    int FailureCount { get; }

    /// <summary>Count of recorded successes since last trip or reset.</summary>
    int SuccessCount { get; }

    /// <summary>Timestamp in UTC of the most recent state change.</summary>
    DateTimeOffset LastStateChangeUtc { get; }

    /// <summary>Timestamp in UTC of the most recent failure, if any.</summary>
    DateTimeOffset? LastFailureUtc { get; }

    /// <summary>Message of the most recent exception recorded.</summary>
    string? LastExceptionMessage { get; }

    /// <summary>Checks whether a call is currently allowed through.</summary>
    bool CanExecute();

    /// <summary>Executes the given asynchronous function under circuit breaker protection.</summary>
    Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default);

    /// <summary>Executes the given asynchronous action under circuit breaker protection.</summary>
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>Explicitly records a successful execution.</summary>
    void RecordSuccess();

    /// <summary>Explicitly records an execution failure.</summary>
    void RecordFailure(Exception ex);

    /// <summary>Manually trips the circuit breaker to the Open state.</summary>
    void Trip();

    /// <summary>Manually resets the circuit breaker to the Closed state.</summary>
    void Reset();
}
