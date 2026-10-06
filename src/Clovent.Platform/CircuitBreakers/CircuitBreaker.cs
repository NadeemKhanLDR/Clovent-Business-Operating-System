namespace Clovent.Platform.CircuitBreakers;

/// <summary>Configuration options for a circuit breaker.</summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>Number of consecutive failures that trip the circuit from Closed to Open. Default is 3.</summary>
    public int FailureThreshold { get; set; } = 3;

    /// <summary>Duration to remain in the Open state before allowing a trial call in HalfOpen. Default is 30 seconds.</summary>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Alias for OpenDuration for backwards compatibility.</summary>
    public TimeSpan BreakDuration { get => OpenDuration; set => OpenDuration = value; }

    /// <summary>Number of successful trial executions in HalfOpen required to transition back to Closed. Default is 1.</summary>
    public int HalfOpenSuccessThreshold { get; set; } = 1;
}

/// <summary>Thread-safe circuit breaker protecting against cascading failures.</summary>
public sealed class CircuitBreaker : ICircuitBreaker
{
    private readonly object _lock = new();
    private readonly CircuitBreakerOptions _options;

    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount;
    private int _successCount;
    private DateTimeOffset _lastStateChangeUtc = DateTimeOffset.UtcNow;
    private DateTimeOffset? _lastFailureUtc;
    private string? _lastExceptionMessage;

    /// <summary>Creates a new named circuit breaker.</summary>
    public CircuitBreaker(string name, CircuitBreakerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        _options = options ?? new CircuitBreakerOptions();
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                CheckHalfOpenTransition();
                return _state;
            }
        }
    }

    /// <inheritdoc/>
    public int FailureCount { get { lock (_lock) return _failureCount; } }

    /// <inheritdoc/>
    public int SuccessCount { get { lock (_lock) return _successCount; } }

    /// <inheritdoc/>
    public DateTimeOffset LastStateChangeUtc { get { lock (_lock) return _lastStateChangeUtc; } }

    /// <inheritdoc/>
    public DateTimeOffset? LastFailureUtc { get { lock (_lock) return _lastFailureUtc; } }

    /// <inheritdoc/>
    public string? LastExceptionMessage { get { lock (_lock) return _lastExceptionMessage; } }

    /// <inheritdoc/>
    public bool CanExecute()
    {
        lock (_lock)
        {
            CheckHalfOpenTransition();
            return _state != CircuitBreakerState.Open;
        }
    }

    /// <inheritdoc/>
    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (_lock)
        {
            CheckHalfOpenTransition();
            if (_state == CircuitBreakerState.Open)
            {
                var remaining = _options.OpenDuration - (DateTimeOffset.UtcNow - _lastStateChangeUtc);
                throw new CircuitBreakerOpenException(Name, remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, _lastExceptionMessage);
            }
        }

        try
        {
            var result = await action().ConfigureAwait(false);
            RecordSuccess();
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            RecordFailure(ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await ExecuteAsync<object?>(async () =>
        {
            await action().ConfigureAwait(false);
            return null;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _lastExceptionMessage = null;
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _successCount++;
                if (_successCount >= _options.HalfOpenSuccessThreshold)
                {
                    _state = CircuitBreakerState.Closed;
                    _failureCount = 0;
                    _successCount = 0;
                    _lastStateChangeUtc = DateTimeOffset.UtcNow;
                }
            }
            else if (_state == CircuitBreakerState.Closed)
            {
                _failureCount = 0;
            }
        }
    }

    /// <inheritdoc/>
    public void RecordFailure(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        lock (_lock)
        {
            _lastFailureUtc = DateTimeOffset.UtcNow;
            _lastExceptionMessage = ex.Message;
            _failureCount++;

            if (_state == CircuitBreakerState.HalfOpen || _failureCount >= _options.FailureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _lastStateChangeUtc = DateTimeOffset.UtcNow;
                _successCount = 0;
            }
        }
    }

    /// <inheritdoc/>
    public void Trip()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Open;
            _lastStateChangeUtc = DateTimeOffset.UtcNow;
            _failureCount = _options.FailureThreshold;
            _successCount = 0;
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _successCount = 0;
            _lastExceptionMessage = null;
            _lastStateChangeUtc = DateTimeOffset.UtcNow;
        }
    }

    private void CheckHalfOpenTransition()
    {
        if (_state == CircuitBreakerState.Open && (DateTimeOffset.UtcNow - _lastStateChangeUtc) >= _options.OpenDuration)
        {
            _state = CircuitBreakerState.HalfOpen;
            _lastStateChangeUtc = DateTimeOffset.UtcNow;
            _successCount = 0;
        }
    }
}
