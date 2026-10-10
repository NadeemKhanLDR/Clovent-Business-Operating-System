using System.ComponentModel;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Printing;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Printing;

/// <summary>
/// Back-Office Document View hosting <see cref="PrinterHardwareHealthControl"/>.
/// Compatible with TabbedView document manager, navigation ribbon, and High-DPI scaling.
/// </summary>
[DesignerCategory("Code")]
public sealed class PrinterHardwareHealthView : BaseForm
{
    private readonly PrinterHardwareHealthControl _control;

    /// <summary>Underlying peripheral health control instance.</summary>
    public PrinterHardwareHealthControl HealthControl => _control;

    /// <summary>Designer parameterless constructor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public PrinterHardwareHealthView() : this(null, null, null, null)
    {
    }

    /// <summary>DI runtime constructor.</summary>
    public PrinterHardwareHealthView(
        PrinterManagementService? printerService,
        IPrintJobQuarantineStore? quarantineStore = null,
        ICircuitBreakerRegistry? circuitBreakerRegistry = null,
        ILogger<PrinterHardwareHealthControl>? logger = null)
    {
        Text = "Peripheral Health & Spooler Monitor";

        _control = new PrinterHardwareHealthControl(
            printerService,
            quarantineStore,
            circuitBreakerRegistry,
            logger)
        {
            Dock = DockStyle.Fill
        };

        ContentPanel.Controls.Add(_control);
    }
}
