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

    /// <summary>Runs <paramref name="operation"/> once every earlier caller through this gate has finished.</summary>
    public async Task<TResult> RunAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await operation();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Runs <paramref name="operation"/> once every earlier caller through this gate has finished.</summary>
    public async Task RunAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await operation();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _gate.Dispose();
}
