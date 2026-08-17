using Clovent.Desktop.Forms.Base;
using Clovent.Identity.Application.Authorization;
using MediatR;
using Xunit;

namespace Clovent.Desktop.Tests.Forms.Base;

/// <summary>
/// Reproduces, deterministically and without a real database, the exact
/// "DbContext concurrency" crash the Restaurant POS screen still hit after
/// <see cref="SerializedMediator"/> alone had already been introduced - and
/// proves <see cref="ScreenOperationGate"/>/<see cref="SerializedFeatureAuthorizationPolicy"/>
/// close the gap. See <see cref="SerializedMediator"/>'s own doc comment for
/// the full root-cause explanation this test class exists to verify.
/// </summary>
public class SerializedMediatorConcurrencyTests
{
    /// <summary>
    /// Stands in for the one EF Core <c>DbContext</c> instance a screen's DI
    /// scope resolves, shared by every scoped service that scope hands out -
    /// throws the same message EF Core itself throws
    /// ("A second operation was started on this context instance before a
    /// previous operation completed.") the instant a second caller enters
    /// while a first is still inside, so this test doesn't need SQL Server
    /// to prove the race is real.
    /// </summary>
    private sealed class RacyScope
    {
        private int _inFlight;

        public async Task EnterAsync()
        {
            if (Interlocked.Increment(ref _inFlight) > 1)
            {
                Interlocked.Decrement(ref _inFlight);
                throw new InvalidOperationException("A second operation was started on this context instance before a previous operation completed.");
            }

            try
            {
                // Widens the race window past a single synchronous
                // continuation - the same way a real DbContext call actually
                // awaits a database round trip, giving a second caller a
                // real chance to interleave.
                await Task.Delay(5);
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }
    }

    private sealed class RacyMediator(RacyScope scope) : IMediator
    {
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            await scope.EnterAsync();
            return default!;
        }

        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            await scope.EnterAsync();

        public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            await scope.EnterAsync();
            return null;
        }

        public async Task Publish(object notification, CancellationToken cancellationToken = default) =>
            await scope.EnterAsync();

        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
            await scope.EnterAsync();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RacyFeatureAuthorizationPolicy(RacyScope scope) : IFeatureAuthorizationPolicy
    {
        public async Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default)
        {
            await scope.EnterAsync();
            return true;
        }
    }

    private sealed record DummyCommand : IRequest<bool>;

    private static Task RunPermissionCheckLoopAsync(IFeatureAuthorizationPolicy featurePolicy) =>
        RunPermissionCheckLoopCoreAsync(featurePolicy);

    private static async Task RunPermissionCheckLoopCoreAsync(IFeatureAuthorizationPolicy featurePolicy)
    {
        // Mirrors RestaurantPosView.UpdatePermissionsAsync: ~20 sequential,
        // awaited CanUseFeatureAsync calls against the screen's own scope.
        for (var i = 0; i < 20; i++)
        {
            await featurePolicy.CanUseFeatureAsync(Guid.NewGuid(), $"pos.op{i}");
        }
    }

    /// <summary>
    /// Before this fix: <see cref="SerializedMediator"/> serializes every
    /// <c>Send</c>/<c>Publish</c> call, but a screen's own
    /// <c>IFeatureAuthorizationPolicy</c> (resolved from the same DI scope,
    /// never routed through the mediator) was never gated at all. Two
    /// independently-triggered operations - a permission-check loop
    /// (<c>RefreshOrderAsync</c>'s <c>UpdatePermissionsAsync</c>) and a
    /// button click's mediator <c>Send</c> - starting close enough together
    /// still race on the shared scope and throw, exactly matching the
    /// screenshot: gating the mediator alone did not fix it.
    /// </summary>
    [Fact]
    public async Task MediatorAloneGated_FeaturePolicyUngated_ConcurrentOperationsRaceOnSharedScope()
    {
        var scope = new RacyScope();
        var mediatorOnlyGate = new ScreenOperationGate();
        IMediator mediator = new SerializedMediator(new RacyMediator(scope), mediatorOnlyGate);
        // The bug, reproduced exactly: the feature policy resolved straight
        // from the scope, with no gate at all - what every Restaurant
        // screen did before this fix.
        IFeatureAuthorizationPolicy featurePolicy = new RacyFeatureAuthorizationPolicy(scope);

        var permissionLoop = RunPermissionCheckLoopAsync(featurePolicy);
        var buttonClick = mediator.Send(new DummyCommand());

        var thrown = await Record.ExceptionAsync(() => Task.WhenAll(permissionLoop, buttonClick));

        Assert.NotNull(thrown);
        Assert.Contains("A second operation was started", thrown!.Message);
    }

    /// <summary>
    /// The fix: <see cref="SerializedMediator"/> and
    /// <see cref="SerializedFeatureAuthorizationPolicy"/> sharing one
    /// <see cref="ScreenOperationGate"/> serializes both call paths against
    /// each other, so the identical two operations from the test above -
    /// permission-check loop racing a button click's <c>Send</c> - can never
    /// overlap. Run 50 times (each with a fresh gate/scope) rather than once,
    /// so a fix that merely narrows the race window instead of closing it
    /// would still be caught.
    /// </summary>
    [Fact]
    public async Task SharedGate_MediatorAndFeaturePolicyTogether_NeverRaceOnSharedScope()
    {
        for (var iteration = 0; iteration < 50; iteration++)
        {
            var scope = new RacyScope();
            var gate = new ScreenOperationGate();
            IMediator mediator = new SerializedMediator(new RacyMediator(scope), gate);
            IFeatureAuthorizationPolicy featurePolicy = new SerializedFeatureAuthorizationPolicy(new RacyFeatureAuthorizationPolicy(scope), gate);

            var permissionLoop = RunPermissionCheckLoopAsync(featurePolicy);
            var buttonClick = mediator.Send(new DummyCommand());

            await Task.WhenAll(permissionLoop, buttonClick);
        }
    }
}
