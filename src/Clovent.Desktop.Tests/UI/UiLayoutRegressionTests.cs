using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Restaurant.Setup;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Notifications;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using MediatR;
using Moq;
using Xunit;

namespace Clovent.Desktop.Tests.UI;

/// <summary>
/// Regression assertions for the high-DPI WinForms layout and readability fixes:
/// 1. Restaurant Setup View (POS Setup) starting number, active orders, default order mode, and non-overlapping controls.
/// 2. CommandPanelLayout editor anchoring, minimum height, and section heading AutoSizeMode.
/// 3. Notifications form sizing, readability, minimum dimensions, and accessible Close action.
/// 4. OrganizationHierarchySelector & EntityPicker vertical centering, dropdown rows, and DPI-scaled widths.
/// </summary>
public sealed class UiLayoutRegressionTests
{
    private static T GetField<T>(object instance, string name) where T : class
    {
        var type = instance.GetType();
        while (type != null)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) return (T)field.GetValue(instance)!;
            type = type.BaseType;
        }
        throw new Exception($"Field {name} not found on {instance.GetType().Name}");
    }

    [Fact]
    public void NotificationsForm_HasAppropriateInitialAndMinimumSize_AndAccessibleCloseButton()
    {
        var notifications = new List<Notification>
        {
            new("System Update", "System restarted successfully at 10:00 UTC", DateTime.UtcNow)
        };

        using var form = new NotificationsForm(notifications);
        form.CreateControl();

        var list = GetField<ListBoxControl>(form, "_notificationsList");
        var closeButton = GetField<SimpleButton>(form, "_closeButton");

        Assert.NotNull(list);
        Assert.NotNull(closeButton);

        // Minimum dimensions
        Assert.True(form.MinimumSize.Width >= 440, $"Notifications minimum width {form.MinimumSize.Width} should be >= 440");
        Assert.True(form.MinimumSize.Height >= 320, $"Notifications minimum height {form.MinimumSize.Height} should be >= 320");

        // Client size
        Assert.True(form.ClientSize.Width >= 440, $"Notifications width {form.ClientSize.Width} should be >= 440");
        Assert.True(form.ClientSize.Height >= 320, $"Notifications height {form.ClientSize.Height} should be >= 320");

        // Readable list items
        Assert.True(list.ItemHeight >= 34, $"Notifications list item height {list.ItemHeight} should be >= 34");

        // Accessible close action
        Assert.Equal(DialogResult.OK, closeButton.DialogResult);
        Assert.Equal("Close", closeButton.Text);
        Assert.Same(closeButton, form.CancelButton);
    }

    [Fact]
    public void RestaurantSetupView_NumberingAndPosControls_HaveProperHeightsAndCenterAlignment()
    {
        using var view = new RestaurantSetupView();
        view.CreateControl();

        var prefixEdit = GetField<TextEdit>(view, "_prefixEdit");
        var startingNumberEdit = GetField<SpinEdit>(view, "_startingNumberEdit");
        var activeOrdersRadio = GetField<RadioGroup>(view, "_activeOrdersRadioGroup");
        var defaultOrderModeRadio = GetField<RadioGroup>(view, "_defaultOrderModeRadioGroup");
        var defaultPaymentMethod = GetField<ComboBoxEdit>(view, "_defaultPaymentMethodCombo");

        Assert.NotNull(prefixEdit);
        Assert.NotNull(startingNumberEdit);
        Assert.NotNull(activeOrdersRadio);
        Assert.NotNull(defaultOrderModeRadio);
        Assert.NotNull(defaultPaymentMethod);

        // Starting number must be vertically centered and near-aligned for readability
        Assert.Equal(VertAlignment.Center, startingNumberEdit.Properties.Appearance.TextOptions.VAlignment);
        Assert.Equal(HorzAlignment.Near, startingNumberEdit.Properties.Appearance.TextOptions.HAlignment);

        // Radio groups must not be constrained by restrictive maximum size
        Assert.Equal(Size.Empty, activeOrdersRadio.MaximumSize);
        Assert.Equal(Size.Empty, defaultOrderModeRadio.MaximumSize);
        Assert.True(activeOrdersRadio.Height >= 32, $"Active orders height {activeOrdersRadio.Height} must be >= 32");
        Assert.True(defaultOrderModeRadio.Height >= 32, $"Default order mode height {defaultOrderModeRadio.Height} must be >= 32");

        // Payment method combo dropdown styling
        Assert.Equal(VertAlignment.Center, defaultPaymentMethod.Properties.Appearance.TextOptions.VAlignment);
        Assert.True(defaultPaymentMethod.Properties.DropDownRows >= 8);
    }

    [Fact]
    public void CommandPanelLayout_AddEditor_UsesNoneAnchor_AndHasMinimumHeight()
    {
        using var host = new UserControl();
        var content = new Control();
        var commandFlow = CommandPanelLayout.Build(host, content);

        var editor = new ComboBoxEdit();
        CommandPanelLayout.AddEditor(commandFlow, editor);

        Assert.Equal(AnchorStyles.None, editor.Anchor);
        Assert.True(editor.MinimumSize.Height >= 30, $"Editor minimum height {editor.MinimumSize.Height} must be >= 30");

        var heading = CommandPanelLayout.BuildSectionHeading("Actions");
        Assert.Equal(LabelAutoSizeMode.Default, heading.AutoSizeMode);
    }

    [Fact]
    public void OrganizationHierarchySelector_CombosAndLabels_AreVerticallyCentered_AndWidthIsScaled()
    {
        var mediatorMock = new Mock<IMediator>();
        using var selector = new OrganizationHierarchySelector(mediatorMock.Object, showCompany: true, showBranch: true);
        selector.CreateControl();

        var orgCombo = GetField<ComboBoxEdit>(selector, "_organizationCombo");
        var compCombo = GetField<ComboBoxEdit>(selector, "_companyCombo");
        var branchCombo = GetField<ComboBoxEdit>(selector, "_branchCombo");

        foreach (var combo in new[] { orgCombo, compCombo, branchCombo })
        {
            Assert.Equal(VertAlignment.Center, combo.Properties.Appearance.TextOptions.VAlignment);
            Assert.True(combo.Properties.DropDownRows >= 8, "Dropdown rows must be >= 8 to prevent clipped popups");
            Assert.True(combo.Width >= 260, $"Combo width {combo.Width} must be >= 260");
        }
    }

    [Fact]
    public void EntityPicker_CombosAndLabels_AreVerticallyCentered_AndWidthIsScaled()
    {
        using var picker = new EntityPicker("Warehouse:", comboWidth: 260);
        picker.CreateControl();

        var combo = GetField<ComboBoxEdit>(picker, "_combo");
        var label = GetField<LabelControl>(picker, "_label");

        Assert.Equal(VertAlignment.Center, combo.Properties.Appearance.TextOptions.VAlignment);
        Assert.Equal(VertAlignment.Center, label.Appearance.TextOptions.VAlignment);
        Assert.True(combo.Properties.DropDownRows >= 8);
        Assert.True(combo.Width >= 260);
    }
}
