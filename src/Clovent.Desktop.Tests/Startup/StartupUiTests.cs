using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Startup;
using DevExpress.XtraEditors;
using Xunit;

namespace Clovent.Desktop.Tests.Startup;

public class StartupUiTests
{
    [Fact]
    public void StartupWaitForm_HasSufficientWidth_AndAccommodatesFullProductTitle()
    {
        using var form = new StartupWaitForm();

        Assert.True(form.MinimumSize.Width >= 460, "Wait form minimum width must be at least 460px to prevent title truncation.");
        Assert.True(form.ClientSize.Width >= 460);

        form.SetCaption("Clovent Business Operating System");
        form.SetDescription("Starting...");

        // Ensure caption was assigned
        var ppField = typeof(StartupWaitForm).GetField("progressPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(ppField);
        var pp = ppField.GetValue(form) as DevExpress.XtraWaitForm.ProgressPanel;
        Assert.NotNull(pp);
        Assert.Equal("Clovent Business Operating System", pp.Caption);
        Assert.Equal("Starting...", pp.Description);
    }

    [Fact]
    public void ErrorDialogForm_HasAdequateInitialDimensions_AndNonCollidingButtons()
    {
        var ex = new InvalidOperationException("Test exception message for error dialog validation");
        using var form = new ErrorDialogForm(ex, "Unit Test Context");

        Assert.True(form.MinimumSize.Width >= 460, "Error dialog minimum width must be at least 460px.");
        Assert.True(form.ClientSize.Width >= 500, "Error dialog client width must provide ample room for buttons.");

        var buttonPanelField = typeof(ErrorDialogForm).GetField("_buttonPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(buttonPanelField);
        var buttonPanel = buttonPanelField.GetValue(form) as PanelControl;
        Assert.NotNull(buttonPanel);
        Assert.True(buttonPanel.Height >= 40);

        var detailsBtnField = typeof(ErrorDialogForm).GetField("_detailsToggle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var copyBtnField = typeof(ErrorDialogForm).GetField("_copyButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var closeBtnField = typeof(ErrorDialogForm).GetField("_closeButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(detailsBtnField);
        Assert.NotNull(copyBtnField);
        Assert.NotNull(closeBtnField);

        var detailsBtn = detailsBtnField.GetValue(form) as SimpleButton;
        var copyBtn = copyBtnField.GetValue(form) as SimpleButton;
        var closeBtn = closeBtnField.GetValue(form) as SimpleButton;

        Assert.NotNull(detailsBtn);
        Assert.NotNull(copyBtn);
        Assert.NotNull(closeBtn);

        Assert.True(detailsBtn.Width >= 120);
        Assert.True(copyBtn.Width >= 120);
        Assert.True(closeBtn.Width >= 90);
    }

    [Fact]
    public void SplashScreenService_Close_IsIdempotentWhenNotShown()
    {
        var service = new SplashScreenService();
        // Should not throw even when no wait form is active
        service.Close();
        service.Close();
    }
}
