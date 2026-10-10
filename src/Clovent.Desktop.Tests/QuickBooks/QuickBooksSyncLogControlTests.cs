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
}
