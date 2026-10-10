using System.ComponentModel;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.QuickBooks;

/// <summary>
/// Back-Office Document View hosting <see cref="QuickBooksSyncLogControl"/>.
/// Compatible with TabbedView document manager and High-DPI scaling.
/// </summary>
[DesignerCategory("Code")]
public sealed class QuickBooksSyncLogView : BaseForm
{
    private readonly QuickBooksSyncLogControl _control;

    /// <summary>Underlying QuickBooks sync monitor control instance.</summary>
    public QuickBooksSyncLogControl LogControl => _control;

    /// <summary>Designer parameterless constructor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public QuickBooksSyncLogView() : this(null, null, null, null, null)
    {
    }

    /// <summary>DI runtime constructor.</summary>
    public QuickBooksSyncLogView(
        IServiceScopeFactory? scopeFactory,
        IOutboxProcessor? outboxProcessor = null,
        IOutboxRepository? outboxRepository = null,
        ICircuitBreakerRegistry? circuitBreakerRegistry = null,
        ICurrentSession? currentSession = null)
    {
        _control = new QuickBooksSyncLogControl(
            scopeFactory,
            outboxProcessor,
            outboxRepository,
            circuitBreakerRegistry,
            currentSession)
        {
            Dock = DockStyle.Fill
        };

        ContentPanel.Controls.Add(_control);
    }
}
