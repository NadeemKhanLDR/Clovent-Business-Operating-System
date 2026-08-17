using MediatR;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Decorates a scope's <see cref="IMediator"/> so every <c>Send</c>/
/// <see cref="Publish(object,CancellationToken)"/> call made *from that
/// screen's own UI code* is serialized end-to-end, never overlapping -
/// against every other call routed through the *same* <see cref="ScreenOperationGate"/>,
/// including calls made through <see cref="SerializedFeatureAuthorizationPolicy"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Root cause this fixes:</b> a screen's own DI scope resolves exactly
/// one <c>RestaurantDbContext</c> instance, shared by every
/// <c>_mediator.Send(...)</c> call that screen ever makes. EF Core's
/// <c>DbContext</c> is not safe for two operations in flight at the same
/// time - and "at the same time" does not require true multi-threading:
/// two independently-triggered async chains on the very same UI thread
/// (e.g. a button click's <c>await _mediator.Send(A)</c> and an unrelated
/// event handler's <c>await _mediator.Send(B)</c>, both started before
/// either finished) still interleave their continuations on that one
/// <c>SynchronizationContext</c>, and EF Core detects that as "a second
/// operation was started on this context instance before a previous
/// operation completed."
/// </para>
/// <para>
/// <b>Why gating <see cref="IMediator"/> alone was not enough:</b> the same
/// scope's <c>IFeatureAuthorizationPolicy</c> (resolved by every screen to
/// drive its own <c>UpdateButtonStates</c>-style permission checks) hits its
/// own scoped DbContext (Identity's) through a completely separate call path
/// that never goes through <see cref="IMediator"/> at all. Gating only
/// <c>Send</c>/<c>Publish</c> left that path unprotected: two overlapping
/// screen operations - e.g. a cashier clicking two different buttons before
/// the first one's own permission-check loop and refresh had finished, or a
/// background change-notification racing a button click - could each reach
/// their own <c>CanUseFeatureAsync</c> loop at the same time and hit the
/// same scope's DbContext concurrently, throwing the exact same "a second
/// operation was started" exception even with every <c>Send</c> call
/// individually serialized. Wrapping <em>both</em> services in a single
/// shared <see cref="ScreenOperationGate"/> closes that gap: nothing that
/// touches this scope's services - mediator or feature policy - can ever run
/// concurrently with anything else that does.
/// </para>
/// <para>
/// <b>Why this can't deadlock:</b> this wrapper is never registered into
/// the DI container as the <see cref="IMediator"/> implementation handlers
/// receive - it exists only as a screen's own <c>_mediator</c> field,
/// called exclusively from UI code outside the MediatR pipeline. A command
/// handler's own internal work (including <c>UnitOfWorkBehavior</c>
/// publishing domain events after <c>SaveChangesAsync</c>) goes through the
/// real, undecorated <see cref="IMediator"/> resolved from DI, never
/// through this wrapper - so there is no path by which acquiring the gate
/// could recursively try to acquire itself.
/// </para>
/// </remarks>
public sealed class SerializedMediator(IMediator inner, ScreenOperationGate gate) : IMediator
{
    /// <inheritdoc/>
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        gate.RunAsync(() => inner.Send(request, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
        gate.RunAsync(() => inner.Send(request, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
        gate.RunAsync(() => inner.Send(request, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task Publish(object notification, CancellationToken cancellationToken = default) =>
        gate.RunAsync(() => inner.Publish(notification, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
        gate.RunAsync(() => inner.Publish(notification, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        inner.CreateStream(request, cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        inner.CreateStream(request, cancellationToken);
}
