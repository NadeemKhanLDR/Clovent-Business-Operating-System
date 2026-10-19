namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// One screen's single choke point for every async call that touches its own
/// DI scope's services. <see cref="SerializedMediator"/> and
/// <see cref="SerializedFeatureAuthorizationPolicy"/> both serialize through
/// the *same* instance of this gate (constructed once per screen, alongside
/// the screen's own <c>IServiceScope</c>) - see their doc comments for why a
/// screen needs exactly one shared gate rather than each wrapper guarding
/// itself independently.
/// </summary>
public sealed class ScreenOperationGate : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly AsyncLocal<int> _reentrancyCount = new();
    private volatile bool _isDisposed;

    /// <summary>Runs <paramref name="operation"/> once every earlier caller through this gate has finished.</summary>
    public async Task<TResult> RunAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_reentrancyCount.Value > 0)
        {
            _reentrancyCount.Value++;
            try
            {
                return await operation().ConfigureAwait(false);
            }
            finally
            {
                _reentrancyCount.Value--;
            }
        }

        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        _reentrancyCount.Value = 1;
        try
        {
            return await operation().ConfigureAwait(false);
        }
        finally
        {
            _reentrancyCount.Value = 0;
            try
            {
                _gate.Release();
            }
            catch (ObjectDisposedException) { }
        }
    }

    /// <summary>Runs <paramref name="operation"/> once every earlier caller through this gate has finished.</summary>
    public async Task RunAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_reentrancyCount.Value > 0)
        {
            _reentrancyCount.Value++;
            try
            {
                await operation().ConfigureAwait(false);
            }
            finally
            {
                _reentrancyCount.Value--;
            }
            return;
        }

        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        _reentrancyCount.Value = 1;
        try
        {
            await operation().ConfigureAwait(false);
        }
        finally
        {
            _reentrancyCount.Value = 0;
            try
            {
                _gate.Release();
            }
            catch (ObjectDisposedException) { }
        }
    }

    /// <summary>
    /// Waits up to <paramref name="timeout"/> for any running operation to complete.
    /// If acquired, the gate is held so no further operations can start.
    /// </summary>
    public bool Drain(TimeSpan timeout)
    {
        try
        {
            return _gate.Wait(timeout);
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _isDisposed = true;
        try
        {
            _gate.Dispose();
        }
        catch (ObjectDisposedException) { }
    }
}
