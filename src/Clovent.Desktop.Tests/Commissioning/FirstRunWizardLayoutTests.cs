using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Commissioning.UI;
using DevExpress.XtraEditors;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class FirstRunWizardLayoutTests
{
    [Fact]
    public void FirstRunWizardForm_MinimumSize_And_SizableBorder_AreConfigured()
    {
        using var form = new FirstRunWizardForm();
        form.CreateControl();

        Assert.True(form.MinimumSize.Width >= 960, $"MinimumSize.Width was {form.MinimumSize.Width}, expected >= 960");
        Assert.True(form.MinimumSize.Height >= 640, $"MinimumSize.Height was {form.MinimumSize.Height}, expected >= 640");
        Assert.Equal(FormBorderStyle.Sizable, form.FormBorderStyle);
        Assert.True(form.MaximizeBox, "MaximizeBox should be enabled for desktop accessibility");
    }

    [Fact]
    public void FirstRunWizardForm_AutoScroll_EnabledOnAllContentPanels()
    {
        using var form = new FirstRunWizardForm();
        form.CreateControl();

        var containerField = typeof(FirstRunWizardForm).GetField("panelContainer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(containerField);
        var container = (Panel)containerField.GetValue(form)!;
        Assert.True(container.AutoScroll, "panelContainer must have AutoScroll enabled to prevent control clipping");

        for (int step = 1; step <= 8; step++)
        {
            var panelField = typeof(FirstRunWizardForm).GetField($"panelStep{step}", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(panelField);
            var stepPanel = (Panel)panelField.GetValue(form)!;
            Assert.True(stepPanel.AutoScroll, $"panelStep{step} must have AutoScroll enabled to handle high DPI scaling");
        }
    }

    [Fact]
    public void FirstRunWizardForm_Sidebar_WidthAndStepSpacing_AreProperlyProportioned()
    {
        using var form = new FirstRunWizardForm();
        form.Size = new Size(1280, 800);
        form.CreateControl();

        var sidebarField = typeof(FirstRunWizardForm).GetField("panelSidebar", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(sidebarField);
        var sidebar = (Panel)sidebarField.GetValue(form)!;

        Assert.True(sidebar.Width >= 240, $"Sidebar width {sidebar.Width} is too narrow for high-DPI text");

        int previousY = 0;
        for (int i = 1; i <= 8; i++)
        {
            var stepLabelField = typeof(FirstRunWizardForm).GetField($"lblStep{i}", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(stepLabelField);
            var stepLabel = (LabelControl)stepLabelField.GetValue(form)!;

            Assert.True(stepLabel.Top > previousY, $"Step {i} label top ({stepLabel.Top}) is not below previous step ({previousY})");
            if (i > 1)
            {
                int delta = stepLabel.Top - previousY;
                Assert.True(delta >= 30, $"Vertical step spacing {delta}px between steps {i - 1} and {i} is too compressed");
            }
            previousY = stepLabel.Top;
        }
    }

    [Fact]
    public void FirstRunWizardForm_Footer_ButtonsAndStatus_DoNotOverlap()
    {
        using var form = new FirstRunWizardForm();
        form.Size = new Size(1280, 800);
        form.CreateControl();

        var btnCancelField = typeof(FirstRunWizardForm).GetField("btnCancel", BindingFlags.NonPublic | BindingFlags.Instance);
        var btnBackField = typeof(FirstRunWizardForm).GetField("btnBack", BindingFlags.NonPublic | BindingFlags.Instance);
        var btnNextField = typeof(FirstRunWizardForm).GetField("btnNext", BindingFlags.NonPublic | BindingFlags.Instance);
        var btnFinishField = typeof(FirstRunWizardForm).GetField("btnFinish", BindingFlags.NonPublic | BindingFlags.Instance);
        var lblStatusField = typeof(FirstRunWizardForm).GetField("lblFooterStatus", BindingFlags.NonPublic | BindingFlags.Instance);
        var lblIndicatorField = typeof(FirstRunWizardForm).GetField("lblStepIndicator", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(btnCancelField);
        Assert.NotNull(btnBackField);
        Assert.NotNull(btnNextField);
        Assert.NotNull(btnFinishField);
        Assert.NotNull(lblStatusField);
        Assert.NotNull(lblIndicatorField);

        var btnCancel = (SimpleButton)btnCancelField.GetValue(form)!;
        var btnBack = (SimpleButton)btnBackField.GetValue(form)!;
        var btnNext = (SimpleButton)btnNextField.GetValue(form)!;
        var btnFinish = (SimpleButton)btnFinishField.GetValue(form)!;
        var lblStatus = (LabelControl)lblStatusField.GetValue(form)!;
        var lblIndicator = (LabelControl)lblIndicatorField.GetValue(form)!;

        // Button heights must be comfortably touch- and click-friendly
        Assert.True(btnCancel.Height >= 30, $"btnCancel height {btnCancel.Height} too small");
        Assert.True(btnNext.Height >= 30, $"btnNext height {btnNext.Height} too small");

        // Buttons must be positioned rightwards in sequence
        Assert.True(btnCancel.Right <= btnBack.Left, $"btnCancel (right={btnCancel.Right}) overlaps btnBack (left={btnBack.Left})");
        Assert.True(btnBack.Right <= btnNext.Left, $"btnBack (right={btnBack.Right}) overlaps btnNext (left={btnNext.Left})");

        // Status label must not overlap btnCancel
        Assert.True(lblStatus.Right <= btnCancel.Left, $"lblFooterStatus (right={lblStatus.Right}) overlaps btnCancel (left={btnCancel.Left})");
        Assert.True(lblStatus.Left >= lblIndicator.Right, $"lblFooterStatus (left={lblStatus.Left}) overlaps lblStepIndicator (right={lblIndicator.Right})");
    }

    [Theory]
    [InlineData(1020, 720)]
    [InlineData(1366, 768)]
    [InlineData(1600, 900)]
    [InlineData(1920, 1080)]
    public void FirstRunWizardForm_ResponsiveLayout_AdaptsToDifferentWindowSizes(int width, int height)
    {
        using var form = new FirstRunWizardForm();
        form.Size = new Size(width, height);
        form.CreateControl();

        var panelContainerField = typeof(FirstRunWizardForm).GetField("panelContainer", BindingFlags.NonPublic | BindingFlags.Instance);
        var panelHeaderField = typeof(FirstRunWizardForm).GetField("panelHeader", BindingFlags.NonPublic | BindingFlags.Instance);
        var panelBottomField = typeof(FirstRunWizardForm).GetField("panelBottom", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(panelContainerField);
        Assert.NotNull(panelHeaderField);
        Assert.NotNull(panelBottomField);

        var container = (Panel)panelContainerField.GetValue(form)!;
        var header = (Panel)panelHeaderField.GetValue(form)!;
        var footer = (Panel)panelBottomField.GetValue(form)!;

        Assert.True(header.Height >= 70, $"Header height {header.Height} is too small");
        Assert.True(footer.Height >= 55, $"Footer height {footer.Height} is too small");
        Assert.True(container.Width > 500, $"Container width {container.Width} is too constrained");
        Assert.True(container.Height > 400, $"Container height {container.Height} is too constrained");
    }

    [Fact]
    public void FirstRunWizardForm_StepNavigation_MaintainsUsableContentAcrossAllSteps()
    {
        using var form = new FirstRunWizardForm();
        form.Size = new Size(1366, 768);
        form.CreateControl();

        var setStepMethod = typeof(FirstRunWizardForm).GetMethod("SetStep", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(setStepMethod);

        form.Visible = true;

        for (int step = 1; step <= 8; step++)
        {
            setStepMethod.Invoke(form, [step]);

            // Check that active step panel is visible, others are not
            for (int s = 1; s <= 8; s++)
            {
                var panelField = typeof(FirstRunWizardForm).GetField($"panelStep{s}", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(panelField);
                var panel = (Panel)panelField.GetValue(form)!;

                if (s == step)
                {
                    Assert.True(panel.Visible, $"panelStep{s} should be visible on step {step}");
                    Assert.True(panel.ClientSize.Width > 0, $"panelStep{s} width must be positive");
                }
                else
                {
                    Assert.False(panel.Visible, $"panelStep{s} should be hidden on step {step}");
                }
            }
        }
    }
}
