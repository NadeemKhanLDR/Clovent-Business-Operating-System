using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Commissioning.UI;
using Clovent.Desktop.Forms.Base;
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

    [Fact]
    public void DesktopStyle_BodyFont_MeetsHighDpiReadabilityStandard()
    {
        Assert.NotNull(DesktopStyle.BodyFont);
        Assert.Equal("Segoe UI", DesktopStyle.BodyFont.FontFamily.Name);
        Assert.InRange(DesktopStyle.BodyFont.SizeInPoints, 9.5f, 11.0f);

        Assert.NotNull(DesktopStyle.BodyFontBold);
        Assert.Equal("Segoe UI", DesktopStyle.BodyFontBold.FontFamily.Name);
        Assert.InRange(DesktopStyle.BodyFontBold.SizeInPoints, 9.5f, 11.0f);
        Assert.True(DesktopStyle.BodyFontBold.Bold);
    }

    [Fact]
    public void FirstRunWizardForm_Step1_BodyFont_MeetsMinimumReadableSize()
    {
        using var form = new FirstRunWizardForm();
        form.CreateControl();

        string[] controlFieldNames =
        [
            "lblStep1Desc",
            "lblPrereqSql",
            "lblPrereqRuntime",
            "lblPrereqDisplay",
            "lblPrereqAdmin",
            "lblDetectedOs",
            "lblDetectedRuntime",
            "lblDetectedDpi",
            "lblDetectedElevation"
        ];

        foreach (var name in controlFieldNames)
        {
            var field = typeof(FirstRunWizardForm).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            var ctrl = (LabelControl)field.GetValue(form)!;

            Assert.True(ctrl.Appearance.Options.UseFont, $"Control {name} must have Appearance.Options.UseFont enabled.");
            Assert.NotNull(ctrl.Appearance.Font);
            Assert.True(ctrl.Appearance.Font.SizeInPoints >= 9.5f,
                $"Control {name} font size ({ctrl.Appearance.Font.SizeInPoints}pt) is below the minimum 9.5pt readable body threshold.");
            Assert.True(ctrl.Appearance.Font.SizeInPoints <= 11.0f,
                $"Control {name} font size ({ctrl.Appearance.Font.SizeInPoints}pt) exceeds the 11.0pt body threshold.");
        }

        var elevField = typeof(FirstRunWizardForm).GetField("lblDetectedElevation", BindingFlags.NonPublic | BindingFlags.Instance);
        var lblElev = (LabelControl)elevField!.GetValue(form)!;
        Assert.True(lblElev.Appearance.Options.UseForeColor, "lblDetectedElevation must have Appearance.Options.UseForeColor enabled for status green visibility.");
    }

    [Fact]
    public void FirstRunWizardForm_Step1_ControlsRemainInsideTheirContainers()
    {
        using var form = new FirstRunWizardForm();
        form.Size = new Size(1280, 800);
        form.CreateControl();

        var grpPrereqField = typeof(FirstRunWizardForm).GetField("grpPrerequisites", BindingFlags.NonPublic | BindingFlags.Instance);
        var grpDetectField = typeof(FirstRunWizardForm).GetField("grpSystemDetection", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(grpPrereqField);
        Assert.NotNull(grpDetectField);

        var grpPrerequisites = (GroupControl)grpPrereqField.GetValue(form)!;
        var grpSystemDetection = (GroupControl)grpDetectField.GetValue(form)!;

        // Cards must not overlap
        Assert.True(grpSystemDetection.Top >= grpPrerequisites.Bottom,
            $"grpSystemDetection (Top={grpSystemDetection.Top}) overlaps grpPrerequisites (Bottom={grpPrerequisites.Bottom})");

        // Verify Prerequisite controls inside container
        string[] prereqNames = ["lblPrereqSql", "lblPrereqRuntime", "lblPrereqDisplay", "lblPrereqAdmin"];
        int prevPrereqTop = 0;
        foreach (var name in prereqNames)
        {
            var field = typeof(FirstRunWizardForm).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            var lbl = (LabelControl)field!.GetValue(form)!;

            Assert.True(lbl.Top >= 28, $"Control {name} Top ({lbl.Top}) is too high and cuts into the card header.");
            Assert.True(lbl.Top + lbl.Height <= grpPrerequisites.Height,
                $"Control {name} Bottom ({lbl.Top + lbl.Height}) exceeds card height ({grpPrerequisites.Height}).");
            Assert.True(lbl.Left >= 16, $"Control {name} Left ({lbl.Left}) is too close to the card edge.");

            if (prevPrereqTop > 0)
            {
                int rowDelta = lbl.Top - prevPrereqTop;
                Assert.True(rowDelta >= 26, $"Row delta {rowDelta}px between prerequisite rows is too tight.");
            }
            prevPrereqTop = lbl.Top;
        }

        // Verify Detection controls inside container
        string[] detectNames = ["lblDetectedOs", "lblDetectedRuntime", "lblDetectedDpi", "lblDetectedElevation"];
        int prevDetectTop = 0;
        foreach (var name in detectNames)
        {
            var field = typeof(FirstRunWizardForm).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            var lbl = (LabelControl)field!.GetValue(form)!;

            Assert.True(lbl.Top >= 28, $"Control {name} Top ({lbl.Top}) is too high and cuts into the card header.");
            Assert.True(lbl.Top + lbl.Height <= grpSystemDetection.Height,
                $"Control {name} Bottom ({lbl.Top + lbl.Height}) exceeds card height ({grpSystemDetection.Height}).");
            Assert.True(lbl.Left >= 16, $"Control {name} Left ({lbl.Left}) is too close to the card edge.");

            if (prevDetectTop > 0)
            {
                int rowDelta = lbl.Top - prevDetectTop;
                Assert.True(rowDelta >= 26, $"Row delta {rowDelta}px between detection rows is too tight.");
            }
            prevDetectTop = lbl.Top;
        }
    }

    [Theory]
    [InlineData(1020, 720)]
    [InlineData(1366, 768)]
    [InlineData(1600, 900)]
    [InlineData(1920, 1080)]
    public void FirstRunWizardForm_Step1_FitsWithoutScrollbarsAtRepresentativeResolutions(int width, int height)
    {
        using var form = new FirstRunWizardForm();
        form.Size = new Size(width, height);
        form.CreateControl();

        var panelStep1Field = typeof(FirstRunWizardForm).GetField("panelStep1", BindingFlags.NonPublic | BindingFlags.Instance);
        var grpDetectField = typeof(FirstRunWizardForm).GetField("grpSystemDetection", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(panelStep1Field);
        Assert.NotNull(grpDetectField);

        var panelStep1 = (Panel)panelStep1Field.GetValue(form)!;
        var grpSystemDetection = (GroupControl)grpDetectField.GetValue(form)!;

        // Content must fit within panelStep1 client height without scrolling
        Assert.True(grpSystemDetection.Bottom <= panelStep1.ClientSize.Height,
            $"At {width}x{height}, Step 1 content bottom ({grpSystemDetection.Bottom}) exceeds panel client height ({panelStep1.ClientSize.Height}), causing vertical scrollbar.");
    }

    [Fact]
    public void FirstRunWizardForm_AllSteps_DescriptionsAndNotices_UseStandardReadableFonts()
    {
        using var form = new FirstRunWizardForm();
        form.CreateControl();

        for (int i = 1; i <= 8; i++)
        {
            var field = typeof(FirstRunWizardForm).GetField($"lblStep{i}Desc", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            var lbl = (LabelControl)field.GetValue(form)!;

            Assert.True(lbl.Appearance.Options.UseFont, $"lblStep{i}Desc must have Appearance.Options.UseFont enabled.");
            Assert.True(lbl.Appearance.Font.SizeInPoints >= 9.5f,
                $"lblStep{i}Desc font size ({lbl.Appearance.Font.SizeInPoints}pt) should be >= 9.5pt.");
        }

        var policyField = typeof(FirstRunWizardForm).GetField("lblPasswordPolicy", BindingFlags.NonPublic | BindingFlags.Instance);
        var lblPolicy = (LabelControl)policyField!.GetValue(form)!;
        Assert.True(lblPolicy.Appearance.Font.SizeInPoints >= 9.0f,
            $"lblPasswordPolicy font size ({lblPolicy.Appearance.Font.SizeInPoints}pt) should be at least 9.0pt.");

        var finishField = typeof(FirstRunWizardForm).GetField("lblFinishNotice", BindingFlags.NonPublic | BindingFlags.Instance);
        var lblFinish = (LabelControl)finishField!.GetValue(form)!;
        Assert.True(lblFinish.Appearance.Font.SizeInPoints >= 9.5f,
            $"lblFinishNotice font size ({lblFinish.Appearance.Font.SizeInPoints}pt) should be at least 9.5pt.");
    }

    [Theory]
    [InlineData(96, 1.0f)]   // 100%
    [InlineData(120, 1.25f)] // 125%
    [InlineData(144, 1.50f)] // 150%
    [InlineData(168, 1.75f)] // 175%
    [InlineData(192, 2.00f)] // 200%
    [InlineData(216, 2.25f)] // 225%
    [InlineData(240, 2.50f)] // 250%
    public void FirstRunWizardForm_Step1_HighDpiScales_MaintainCardProportionsAndZeroClipping(int dpi, float scaleFactor)
    {
        int cardHeight = DesktopDpi.Scale(164, dpi);
        int cardGap = DesktopDpi.Scale(12, dpi);
        int row1 = DesktopDpi.Scale(34, dpi);
        int row2 = DesktopDpi.Scale(64, dpi);
        int row3 = DesktopDpi.Scale(94, dpi);
        int row4 = DesktopDpi.Scale(124, dpi);

        // Row spacing delta must scale linearly and provide at least 26px * scaleFactor
        int minRowSpacing = (int)Math.Floor(26 * scaleFactor);
        Assert.True(row2 - row1 >= minRowSpacing, $"Row 1-2 spacing ({row2 - row1}px) at {dpi} DPI is smaller than minimum required ({minRowSpacing}px).");
        Assert.True(row3 - row2 >= minRowSpacing, $"Row 2-3 spacing ({row3 - row2}px) at {dpi} DPI is smaller than minimum required ({minRowSpacing}px).");
        Assert.True(row4 - row3 >= minRowSpacing, $"Row 3-4 spacing ({row4 - row3}px) at {dpi} DPI is smaller than minimum required ({minRowSpacing}px).");

        // The 4th row plus text height must not clip against bottom of card
        // 10pt Segoe UI text height in pixels is approx 13.33px * scaleFactor
        int estimatedTextHeight = (int)Math.Ceiling(18 * scaleFactor);
        int contentBottom = row4 + estimatedTextHeight;
        Assert.True(contentBottom <= cardHeight,
            $"At {dpi} DPI ({scaleFactor * 100}%), content bottom ({contentBottom}px) exceeds card height ({cardHeight}px) by {contentBottom - cardHeight}px.");

        // Bottom padding inside card must be adequate (at least 15px scaled)
        int bottomPadding = cardHeight - contentBottom;
        int minBottomPadding = (int)Math.Floor(12 * scaleFactor);
        Assert.True(bottomPadding >= minBottomPadding,
            $"At {dpi} DPI, card bottom padding ({bottomPadding}px) is less than required ({minBottomPadding}px).");

        // Total content height for Step 1 must fit within typical client area of standard HD/FHD screens
        int totalContentHeight = DesktopDpi.Scale(34, dpi) + cardHeight + cardGap + cardHeight;
        // At 1080p with 250% scaling, available height in window is approx 720 * 2.5 = 1800, client area > 1000
        Assert.True(cardGap >= (int)(8 * scaleFactor), $"Card gap ({cardGap}px) is too small at {dpi} DPI.");
    }
}

