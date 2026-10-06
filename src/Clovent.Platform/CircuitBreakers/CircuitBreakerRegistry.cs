using System.Collections.Concurrent;

namespace Clovent.Platform.CircuitBreakers;

/// <summary>Central registry for named circuit breakers.</summary>
public interface ICircuitBreakerRegistry
{
    /// <summary>Gets or creates a circuit breaker with the specified name and optional default options.</summary>
    ICircuitBreaker GetOrCreate(string name, CircuitBreakerOptions? defaultOptions = null);

    /// <summary>Retrieves all registered circuit breakers.</summary>
    IReadOnlyCollection<ICircuitBreaker> GetAll();
}

/// <summary>Default thread-safe implementation of <see cref="ICircuitBreakerRegistry"/>.</summary>
public sealed class CircuitBreakerRegistry : ICircuitBreakerRegistry
{
    private readonly ConcurrentDictionary<string, ICircuitBreaker> _breakers = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public ICircuitBreaker GetOrCreate(string name, CircuitBreakerOptions? defaultOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _breakers.GetOrAdd(name, n => new CircuitBreaker(n, defaultOptions));
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<ICircuitBreaker> GetAll() => _breakers.Values.ToList().AsReadOnly();
}
