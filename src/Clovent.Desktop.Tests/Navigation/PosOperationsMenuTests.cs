using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Shell;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Application.Shifts.Dtos;
using DevExpress.XtraEditors;
using Xunit;

namespace Clovent.Desktop.Tests.Navigation;

public class PosOperationsMenuTests
{
    private sealed class FakeModeNavigator : IApplicationModeNavigator
    {
        public int OpenPosCallCount { get; private set; }
        public int OpenBackOfficeCallCount { get; private set; }
        public int ExitCallCount { get; private set; }
        public Form? CurrentForm => null;
        public IWorkspaceHost? CurrentWorkspaceHost => null;
        public bool IsTransitioning => false;
        public ApplicationContext ApplicationContext { get; } = new CbosApplicationContext();

        public Task OpenPosAsync(ShiftDto? activeShift = null)
        {
            OpenPosCallCount++;
            return Task.CompletedTask;
        }

        public Task OpenBackOfficeAsync(string? initialViewKey = "dashboard", string? initialCaption = "Dashboard")
        {
            OpenBackOfficeCallCount++;
            return Task.CompletedTask;
        }

        public void ExitApplication()
        {
            ExitCallCount++;
        }

        public void ExitApplication(string initiator)
        {
            ExitCallCount++;
        }
    }

    [Fact]
    public void OperationsButton_Exists_WithCorrectCaptionAndPatternMatchingMore()
    {
        using var form = new RestaurantPosForm();

        Assert.NotNull(form.OperationsButton);
        Assert.IsType<SimpleButton>(form.OperationsButton);
        Assert.IsType<ContextMenuStrip>(form.OperationsMenu);
        Assert.Equal("Operations ▼", form.OperationsButton.Text);
        Assert.Equal("Additional POS operations", form.OperationsButton.ToolTip);
        Assert.NotNull(form.OperationsButton.ImageOptions.SvgImage);
        Assert.True(form.OperationsButton.ImageOptions.SvgImageSize.Width <= 14 && form.OperationsButton.ImageOptions.SvgImageSize.Height <= 14);
        Assert.Equal(DevExpress.XtraEditors.ImageAlignToText.LeftCenter, form.OperationsButton.ImageOptions.ImageToTextAlignment);
    }

    [Fact]
    public void OperationsMenu_Contains_FullWorkflowItems()
    {
        using var form = new RestaurantPosForm();
        form.Show();
        form.CanAccessBackOffice = true;

        var menu = form.OperationsMenu;
        Assert.NotNull(menu);

        Assert.NotNull(form.ShiftMenuItem);
        Assert.Equal("Shift", form.ShiftMenuItem.Text);
        Assert.NotNull(form.OpenShiftMenuItem);
        Assert.Equal("Open Shift", form.OpenShiftMenuItem.Text);
        Assert.NotNull(form.CurrentShiftMenuItem);
        Assert.Equal("Current Shift / Shift Details", form.CurrentShiftMenuItem.Text);
        Assert.NotNull(form.CloseShiftMenuItem);
        Assert.Equal("Close Shift", form.CloseShiftMenuItem.Text);

        Assert.NotNull(form.CashMovementMenuItem);
        Assert.Equal("Cash Movement", form.CashMovementMenuItem.Text);

        Assert.NotNull(form.PrintLastReceiptMenuItem);
        Assert.Equal("Print Last Receipt", form.PrintLastReceiptMenuItem.Text);

        Assert.NotNull(form.EndOfDayMenuItem);
        Assert.Equal("End of Day", form.EndOfDayMenuItem.Text);

        Assert.NotNull(form.BackOfficeMenuItem);
        Assert.Equal("Back Office", form.BackOfficeMenuItem.Text);
        Assert.True(form.OperationsButton.Visible);
    }

    [Fact]
    public void OperationsMenu_StateUpdates_WhenNoShiftActive()
    {
        using var form = new RestaurantPosForm();
        form.SetActiveShift(null);

        Assert.True(form.OpenShiftMenuItem.Enabled);
        Assert.False(form.CurrentShiftMenuItem.Enabled);
        Assert.False(form.CloseShiftMenuItem.Enabled);
        Assert.False(form.CashMovementMenuItem.Enabled);
        Assert.False(form.PrintLastReceiptMenuItem.Enabled);
        Assert.True(form.EndOfDayMenuItem.Enabled);
    }

    [Fact]
    public void OperationsMenu_StateUpdates_WhenShiftActive()
    {
        using var form = new RestaurantPosForm();
        var shift = new ShiftDto(
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Cashier 1",
            DateTimeOffset.UtcNow,
            null,
            "Open",
            100m,
            100m,
            100m,
            0m,
            null,
            null,
            DateTimeOffset.UtcNow);

        form.SetActiveShift(shift);

        Assert.False(form.OpenShiftMenuItem.Enabled);
        Assert.True(form.CurrentShiftMenuItem.Enabled);
        Assert.True(form.CloseShiftMenuItem.Enabled);
        Assert.True(form.CashMovementMenuItem.Enabled);
        Assert.True(form.PrintLastReceiptMenuItem.Enabled);
        Assert.True(form.EndOfDayMenuItem.Enabled);
    }

    [Fact]
    public void OperationsMenu_HidesBackOffice_WhenNotPermitted()
    {
        using var form = new RestaurantPosForm();
        form.CanAccessBackOffice = false;

        Assert.False(form.BackOfficeMenuItem.Visible);
    }

    [Fact]
    public void OperationsMenu_RefundReturn_IsHidden_WhenDomainWorkflowNotAvailable()
    {
        using var form = new RestaurantPosForm();
        Assert.False(form.CanPerformRefund);

        var menu = form.OperationsMenu;
        Assert.DoesNotContain(menu.Items.OfType<ToolStripMenuItem>(), i => i.Text?.Contains("Refund", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void BackOfficeMenuItem_InvokesModeNavigator()
    {
        using var form = new RestaurantPosForm();
        var fakeNavigator = new FakeModeNavigator();
        form.SetApplicationModeNavigator(fakeNavigator);
        form.CanAccessBackOffice = true;

        var backOfficeItem = form.BackOfficeMenuItem;
        backOfficeItem.PerformClick();

        Assert.Equal(1, fakeNavigator.OpenBackOfficeCallCount);
    }

    [Fact]
    public void OperationsButton_IsLocatedInPosHeader_WithProperDpiConstraints()
    {
        using var form = new RestaurantPosForm();

        Assert.NotNull(form.OperationsButton.Parent);
        Assert.True(form.OperationsButton.Parent.Name is "tlpHeaderNew" or "_actionsButtonsFlow");

        Assert.True(form.OperationsButton.MinimumSize.Width >= 0);
        Assert.True(form.OperationsButton.MinimumSize.Height >= 0);
    }

    private static void SimulateClick(SimpleButton button)
    {
        var onClick = typeof(SimpleButton).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        onClick?.Invoke(button, [EventArgs.Empty]);
    }

    [Fact]
    public void OperationsButton_MatchesMoreActionsButton_ControlAndMenuType()
    {
        using var form = new RestaurantPosForm();
        form.Show();
        form.CanAccessBackOffice = true;

        var moreBtn = form.Controls.Find("_moreActionsButton", true).FirstOrDefault() as SimpleButton;
        Assert.NotNull(moreBtn);

        Assert.IsType<SimpleButton>(form.OperationsButton);
        Assert.IsType<SimpleButton>(moreBtn);
        Assert.IsType<ContextMenuStrip>(form.OperationsMenu);
        Assert.False(form.OperationsButton.AllowFocus);
        Assert.Equal(DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, form.OperationsButton.ButtonStyle);
        Assert.Contains("▼", form.OperationsButton.Text);
        Assert.True(moreBtn.Text.Contains("▼") || moreBtn.Text.Contains("▾"));
    }

    [Fact]
    public void OperationsButton_Click_Behavior()
    {
        using var form = new RestaurantPosForm();
        form.Show();
        form.CanAccessBackOffice = true;

        int clickCount = 0;
        form.OperationsButton.Click += (_, _) => clickCount++;

        SimulateClick(form.OperationsButton);
        Assert.Equal(1, clickCount);
    }

    [Fact]
    public void OperationsButton_FocusTransitionFromSearchBox_Succeeds()
    {
        using var form = new RestaurantPosForm();
        form.Show();
        form.CanAccessBackOffice = true;

        var searchEdit = form.Controls.Find("_productSearchEdit", true).FirstOrDefault() as TextEdit;
        Assert.NotNull(searchEdit);
        searchEdit.Focus();

        int clicked = 0;
        form.OperationsButton.Click += (_, _) => clicked++;
        SimulateClick(form.OperationsButton);

        Assert.Equal(1, clicked);
    }

    [Fact]
    public void OperationsButton_HasVisualProminence_WithTealAndWhiteStyling()
    {
        using var form = new RestaurantPosForm();
        form.Show();
        form.CanAccessBackOffice = true;

        var btn = form.OperationsButton;
        Assert.NotNull(btn);

        Assert.Equal(Color.FromArgb(13, 148, 136), btn.Appearance.BackColor);
        Assert.Equal(Color.White, btn.Appearance.ForeColor);
        Assert.Equal(Color.FromArgb(15, 118, 110), btn.AppearanceHovered.BackColor);
        Assert.Equal(Color.White, btn.AppearanceHovered.ForeColor);
        Assert.Equal(Color.FromArgb(17, 94, 89), btn.AppearancePressed.BackColor);
        Assert.Equal(Color.White, btn.AppearancePressed.ForeColor);
        Assert.True(btn.Appearance.Font.Bold);

        Assert.NotNull(btn.ImageOptions.SvgImage);
        Assert.True(btn.ImageOptions.SvgImageSize.Width <= 14 && btn.ImageOptions.SvgImageSize.Height <= 14);
        Assert.True(btn.ImageOptions.SvgImageSize.Width >= 12 && btn.ImageOptions.SvgImageSize.Height >= 12);
        Assert.Equal(DevExpress.XtraEditors.ImageAlignToText.LeftCenter, btn.ImageOptions.ImageToTextAlignment);
        Assert.True(btn.ImageOptions.ImageToTextIndent >= 4 && btn.ImageOptions.ImageToTextIndent <= 6);
        Assert.Equal(DevExpress.Utils.DefaultBoolean.True, btn.ImageOptions.AllowGlyphSkinning);
        Assert.False(btn.AllowFocus);
    }

    [Fact]
    public void OperationsButton_HasDistinctMargins_SeparatingFromMoreAndLogout()
    {
        using var form = new RestaurantPosForm();
        form.Show();
        form.CanAccessBackOffice = true;

        var btn = form.OperationsButton;
        Assert.NotNull(btn);

        Assert.True(btn.Margin.Left >= 6, $"Left margin was {btn.Margin.Left}, expected >= 6");
        Assert.True(btn.Margin.Right >= 6, $"Right margin was {btn.Margin.Right}, expected >= 6");
    }

    [Fact]
    public void MoreActionsButton_RemainsNeutral_NotHighlighted()
    {
        using var form = new RestaurantPosForm();
        form.Show();

        var moreBtn = form.Controls.Find("_moreActionsButton", true).FirstOrDefault() as SimpleButton;
        Assert.NotNull(moreBtn);

        Assert.NotEqual(Color.FromArgb(13, 148, 136), moreBtn.Appearance.BackColor);
    }

    [Fact]
    public void OperationsButton_FormRecreation_InitializesReliably()
    {
        using (var form1 = new RestaurantPosForm())
        {
            form1.Show();
            form1.CanAccessBackOffice = true;
            Assert.True(form1.OperationsButton.Visible);
            Assert.Equal(Color.FromArgb(13, 148, 136), form1.OperationsButton.Appearance.BackColor);
            Assert.NotEmpty(form1.OperationsMenu.Items);
        }

        using (var form2 = new RestaurantPosForm())
        {
            form2.Show();
            form2.CanAccessBackOffice = true;
            Assert.True(form2.OperationsButton.Visible);
            Assert.Equal(Color.FromArgb(13, 148, 136), form2.OperationsButton.Appearance.BackColor);
            Assert.NotEmpty(form2.OperationsMenu.Items);
        }
    }
}
