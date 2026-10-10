using System.ComponentModel;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Sync;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Outbox;

namespace Clovent.Desktop.Sync;

/// <summary>
/// Back-Office Document View hosting <see cref="BranchSyncStatusControl"/>.
/// Compatible with TabbedView document manager and High-DPI scaling.
/// </summary>
[DesignerCategory("Code")]
public sealed class BranchSyncStatusView : BaseForm
{
    private readonly BranchSyncStatusControl _control;

    /// <summary>Underlying sync monitor control instance.</summary>
    public BranchSyncStatusControl SyncControl => _control;

    /// <summary>Designer parameterless constructor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public BranchSyncStatusView() : this(null, null, null, null, null, null, null, null)
    {
    }

    /// <summary>DI runtime constructor.</summary>
    public BranchSyncStatusView(
        ISyncPacketDispatcher? syncDispatcher,
        IOutboxRepository? outboxRepository = null,
        IOutboxProcessor? outboxProcessor = null,
        ICircuitBreakerRegistry? circuitBreakerRegistry = null,
        ISyncConflictStagingStore? conflictStagingStore = null,
        INetworkConnectivityProbe? connectivityProbe = null,
        ISyncIngestionEngine? ingestionEngine = null,
        Clovent.Desktop.Sessions.ICurrentSession? currentSession = null)
    {
        _control = new BranchSyncStatusControl(
            syncDispatcher,
            outboxRepository,
            outboxProcessor,
            circuitBreakerRegistry,
            conflictStagingStore,
            connectivityProbe,
            ingestionEngine,
            currentSession)
        {
            Dock = DockStyle.Fill
        };

        ContentPanel.Controls.Add(_control);
    }
}
