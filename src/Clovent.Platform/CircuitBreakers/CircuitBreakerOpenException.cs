namespace Clovent.Platform.CircuitBreakers;

/// <summary>Exception thrown when an operation is attempted against a tripped/open circuit breaker.</summary>
public sealed class CircuitBreakerOpenException : Exception
{
    /// <summary>The name of the tripped circuit breaker.</summary>
    public string CircuitBreakerName { get; }

    /// <summary>Alias for CircuitBreakerName for convenience.</summary>
    public string CircuitName => CircuitBreakerName;

    /// <summary>Expected time remaining until a trial retry is permitted.</summary>
    public TimeSpan? CooldownRemaining { get; }

    /// <summary>Initializes a new instance of <see cref="CircuitBreakerOpenException"/>.</summary>
    public CircuitBreakerOpenException(string circuitBreakerName, TimeSpan? cooldownRemaining = null, string? lastError = null)
        : base($"Circuit breaker '{circuitBreakerName}' is OPEN. Fast-failing non-critical call to protect system responsiveness. Last error: {lastError ?? "None"}")
    {
        CircuitBreakerName = circuitBreakerName;
        CooldownRemaining = cooldownRemaining;
    }
}
