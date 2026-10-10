using System.Threading;
using Clovent.Desktop.Sync;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Sync;

public sealed class BranchSyncStatusControlTests
{
    [Fact]
    public void ParameterlessConstructor_VisualStudioDesignerSafe_DoesNotThrow()
    {
        // Must succeed without throwing to guarantee Visual Studio WinForms Designer compatibility
        using var control = new BranchSyncStatusControl();
        Assert.NotNull(control);
    }

    [Fact]
    public void RuntimeConstructor_WithDependencies_InitializesLayoutProperly()
    {
        var transport = new InMemoryDeltaSyncTransport();
        var probe = new NetworkConnectivityProbe(initialConnected: true);
        var cbRegistry = new CircuitBreakerRegistry();
        var dispatcher = new DeltaSyncDispatcher(transport, probe, cbRegistry, NullLogger<DeltaSyncDispatcher>.Instance);
        var idempStore = new InMemorySyncIdempotencyStore();
        var stagingStore = new InMemorySyncConflictStagingStore();

        using var control = new BranchSyncStatusControl(
            dispatcher,
            outboxRepository: null,
            outboxProcessor: null,
            circuitBreakerRegistry: cbRegistry,
            conflictStagingStore: stagingStore,
            connectivityProbe: probe);

        Assert.NotNull(control);
        Assert.True(control.Controls.Count > 0);
    }

    [Fact]
    public void BranchSyncStatusView_InitializesContainer_WithDockedControl()
    {
        using var view = new BranchSyncStatusView();
        Assert.NotNull(view);
        Assert.NotNull(view.SyncControl);
    }
}
