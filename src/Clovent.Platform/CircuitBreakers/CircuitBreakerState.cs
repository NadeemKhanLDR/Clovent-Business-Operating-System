namespace Clovent.Platform.CircuitBreakers;

/// <summary>Lifecycle states of a circuit breaker.</summary>
public enum CircuitBreakerState
{
    /// <summary>Normal healthy operation. Calls are executed directly.</summary>
    Closed = 0,

    /// <summary>Tripped due to excessive failures. Calls fail fast without hitting the dependency.</summary>
    Open = 1,

    /// <summary>Cooldown has elapsed; a trial call is permitted to test recovery.</summary>
    HalfOpen = 2
}
