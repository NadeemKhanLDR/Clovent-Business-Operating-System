using System.Windows.Forms;
using Clovent.Desktop.QuickBooks;
using Xunit;

namespace Clovent.Desktop.Tests.QuickBooks;

public sealed class QuickBooksSyncLogControlTests
{
    [Fact]
    public void ParameterlessConstructor_VisualStudioDesignerSafe_DoesNotThrow()
    {
        // Must succeed without throwing to guarantee Visual Studio WinForms Designer compatibility
        using var control = new QuickBooksSyncLogControl();
        Assert.NotNull(control);
    }

    [Fact]
    public void RuntimeConstructor_WithNullDependencies_InitializesLayoutProperly()
    {
        using var control = new QuickBooksSyncLogControl(
            scopeFactory: null,
            outboxProcessor: null,
            outboxRepository: null,
            circuitBreakerRegistry: null,
            currentSession: null);

        Assert.NotNull(control);
        Assert.True(control.Controls.Count > 0);
    }

    [Fact]
    public void QuickBooksSyncLogView_ParameterlessConstructor_VisualStudioDesignerSafe_DoesNotThrow()
    {
        using var view = new QuickBooksSyncLogView();
        Assert.NotNull(view);
        Assert.NotNull(view.LogControl);
        Assert.Equal(DockStyle.Fill, view.LogControl.Dock);
    }

    [Fact]
    public void QuickBooksSyncLogView_RuntimeConstructor_InitializesContainer_WithDockedControl()
    {
        using var view = new QuickBooksSyncLogView(
            scopeFactory: null,
            outboxProcessor: null,
            outboxRepository: null,
            circuitBreakerRegistry: null,
            currentSession: null);

        Assert.NotNull(view);
        Assert.NotNull(view.LogControl);
        Assert.Equal(DockStyle.Fill, view.LogControl.Dock);
    }

    [Fact]
    public void QuickBooksSyncLogView_Layout_EnforcesNonOverlappingSections()
    {
        using var form = new Form { Size = new System.Drawing.Size(1200, 700) };
        using var view = new QuickBooksSyncLogView(null, null, null, null, null) { Dock = DockStyle.Fill };
        form.Controls.Add(view);
        form.Show();

        var control = view.LogControl;
        var header = (DevExpress.XtraEditors.PanelControl)typeof(QuickBooksSyncLogControl).GetField("_headerPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(control)!;
        var kpis = (TableLayoutPanel)typeof(QuickBooksSyncLogControl).GetField("_kpiCardLayout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(control)!;
        var toolbar = (DevExpress.XtraEditors.PanelControl)typeof(QuickBooksSyncLogControl).GetField("_toolbarPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(control)!;
        var grid = (DevExpress.XtraGrid.GridControl)typeof(QuickBooksSyncLogControl).GetField("_gridControl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(control)!;

        Assert.True(header.Height >= 50, $"Header height {header.Height} should be at least 50");
        Assert.True(kpis.Height >= 80, $"KPI cards height {kpis.Height} should be at least 80");
        Assert.True(toolbar.Height >= 38, $"Toolbar height {toolbar.Height} should be at least 38");

        Assert.True(header.Bottom <= kpis.Top, "Header should not overlap with KPI cards");
        Assert.True(kpis.Bottom <= toolbar.Top, "KPI cards should not overlap with Toolbar");
        Assert.True(toolbar.Bottom <= grid.Top, "Toolbar should not overlap with Grid");
    }
}
