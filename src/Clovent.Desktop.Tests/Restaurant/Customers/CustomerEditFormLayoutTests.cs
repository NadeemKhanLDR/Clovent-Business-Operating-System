using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.Customers;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Customers;

public sealed class CustomerEditFormLayoutTests
{
    [Fact]
    public void CustomerEditForm_AllCustomerControls_AreInstantiatedAndParented()
    {
        using var form = new CustomerEditForm("New Customer");
        form.Size = new Size(600, 750);
        form.CreateControl();

        var contentPanelField = typeof(CustomerEditForm).GetField("_contentPanel", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(contentPanelField);
        var contentPanel = (TableLayoutPanel)contentPanelField.GetValue(form)!;
        Assert.NotNull(contentPanel);

        string[] controlFields =
        [
            "_codeEdit",
            "_nameEdit",
            "_mobileEdit",
            "_mobile2Edit",
            "_phoneEdit",
            "_shopNoEdit",
            "_addressEdit",
            "_emailEdit",
            "_openingBalanceEdit",
            "_creditLimitEdit",
            "_isCreditAllowedCheck",
            "_isDefaultCheck",
            "_notesEdit"
        ];

        foreach (var fieldName in controlFields)
        {
            var field = typeof(CustomerEditForm).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.True(field != null, $"Field {fieldName} was not found on CustomerEditForm");

            var ctrl = (Control)field.GetValue(form)!;
            Assert.True(ctrl != null, $"Control {fieldName} was null");
            Assert.True(ctrl.Parent != null, $"Control {fieldName} has null Parent - it was not added to the visual tree!");
            Assert.True(ctrl.Width > 0, $"Control {fieldName} has 0 or negative width");
            Assert.True(ctrl.Height > 0, $"Control {fieldName} has 0 or negative height");
        }
    }

    [Fact]
    public void CustomerEditForm_SectionHeaders_AreAddedToContentPanel()
    {
        using var form = new CustomerEditForm("New Customer");
        form.CreateControl();

        string[] sectionHeaderFields =
        [
            "_lblSectionDetails",
            "_lblSectionAccount",
            "_lblSectionCredit",
            "_lblSectionNotes"
        ];

        var contentPanel = (TableLayoutPanel)typeof(CustomerEditForm)
            .GetField("_contentPanel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;

        foreach (var headerField in sectionHeaderFields)
        {
            var field = typeof(CustomerEditForm).GetField(headerField, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);

            var header = (Control)field.GetValue(form)!;
            Assert.NotNull(header);
            Assert.True(contentPanel.Controls.Contains(header), $"Section header {headerField} is not in content panel");
        }
    }

    [Fact]
    public void CustomerEditForm_PrepopulatesAllValuesCorrectly()
    {
        using var form = new CustomerEditForm(
            title: "Edit Customer",
            code: "CUST-999",
            name: "John Doe",
            mobileNumber: "03001112233",
            address: "123 Main St",
            email: "john@example.com",
            openingBalance: 2500.50m,
            creditLimit: 75000.00m,
            notes: "VIP Client",
            shopNo: "Suite 4B",
            mobile2: "03219988776",
            phone: "04231234567",
            isDefault: true,
            isCreditAllowed: true);

        Assert.Equal("CUST-999", form.CodeValue);
        Assert.Equal("John Doe", form.NameValue);
        Assert.Equal("03001112233", form.MobileValue);
        Assert.Equal("03219988776", form.Mobile2Value);
        Assert.Equal("04231234567", form.PhoneValue);
        Assert.Equal("Suite 4B", form.ShopNoValue);
        Assert.Equal("123 Main St", form.AddressValue);
        Assert.Equal("john@example.com", form.EmailValue);
        Assert.Equal(2500.50m, form.OpeningBalanceValue);
        Assert.Equal(75000.00m, form.CreditLimitValue);
        Assert.True(form.IsCreditAllowedValue);
        Assert.True(form.IsDefaultValue);
        Assert.Equal("VIP Client", form.NotesValue);
    }

    [Fact]
    public void CustomerEditForm_ContentPanel_HasFourColumns_AndNoRunawayAutoSize()
    {
        using var form = new CustomerEditForm("New Customer");
        form.CreateControl();

        var contentPanel = (TableLayoutPanel)typeof(CustomerEditForm)
            .GetField("_contentPanel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;

        Assert.Equal(4, contentPanel.ColumnCount);
        Assert.False(contentPanel.AutoSize, "Content panel should not have AutoSize=true to avoid runaway height and unnecessary vertical scrollbars");
        Assert.Equal(SizeType.Absolute, contentPanel.ColumnStyles[0].SizeType);
        Assert.Equal(SizeType.Percent, contentPanel.ColumnStyles[1].SizeType);
        Assert.Equal(SizeType.Absolute, contentPanel.ColumnStyles[2].SizeType);
        Assert.Equal(SizeType.Percent, contentPanel.ColumnStyles[3].SizeType);
    }

    [Fact]
    public void CustomerEditForm_SectionHeaders_SpanAllFourColumns()
    {
        using var form = new CustomerEditForm("New Customer");
        form.CreateControl();

        var contentPanel = (TableLayoutPanel)typeof(CustomerEditForm)
            .GetField("_contentPanel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;

        string[] sectionHeaderFields =
        [
            "_lblSectionDetails",
            "_lblSectionAccount",
            "_lblSectionCredit",
            "_lblSectionNotes"
        ];

        foreach (var headerField in sectionHeaderFields)
        {
            var field = typeof(CustomerEditForm).GetField(headerField, BindingFlags.NonPublic | BindingFlags.Instance)!;
            var header = (Control)field.GetValue(form)!;
            Assert.Equal(4, contentPanel.GetColumnSpan(header));
        }
    }

    [Fact]
    public void CustomerEditForm_AutoScroll_IsDisabled_And_Layout_IsCompact()
    {
        using var form = new CustomerEditForm("New Customer");
        form.CreateControl();

        var contentPanel = (TableLayoutPanel)typeof(CustomerEditForm)
            .GetField("_contentPanel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;

        Assert.False(contentPanel.AutoScroll, "Content panel AutoScroll must be false to prevent unnecessary vertical scrollbars");
        Assert.True(form.ClientSize.Height <= 500, $"Form height {form.ClientSize.Height} should be <= 500 to avoid empty space below notes");
        Assert.Equal(FormStartPosition.CenterParent, form.StartPosition);
    }

    [Theory]
    [InlineData(96)]
    [InlineData(144)]
    [InlineData(192)]
    [InlineData(240)]
    public void CustomerEditForm_CreditLimitHelperText_DoesNotCollideWithDefaultCustomerCheckbox(int dpi)
    {
        using var form = new CustomerEditForm("New Customer");
        form.CreateControl();

        var contentPanel = (TableLayoutPanel)typeof(CustomerEditForm)
            .GetField("_contentPanel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;

        var pnlCreditLimit = (Control)contentPanel.GetControlFromPosition(1, 9)!;
        Assert.NotNull(pnlCreditLimit);

        var isDefaultCheck = (Control)contentPanel.GetControlFromPosition(1, 10)!;
        Assert.NotNull(isDefaultCheck);

        // Verify that credit limit panel has scaled bottom margin and default checkbox has scaled top margin
        int minMargin = DesktopDpi.Scale(4, dpi);
        int scaledBottomMargin = DesktopDpi.Scale(pnlCreditLimit.Margin.Bottom, dpi);
        int scaledTopMargin = DesktopDpi.Scale(isDefaultCheck.Margin.Top, dpi);

        Assert.True(scaledBottomMargin >= minMargin, $"Credit limit panel scaled bottom margin {scaledBottomMargin} should be >= {minMargin} at {dpi} DPI");
        Assert.True(scaledTopMargin >= minMargin, $"Default customer checkbox scaled top margin {scaledTopMargin} should be >= {minMargin} at {dpi} DPI");
    }

    [Fact]
    public void CustomerEditForm_NotesEditor_IsSingleLineTextEdit_And_ProperlyAligned()
    {
        using var form = new CustomerEditForm("New Customer");
        form.CreateControl();

        var notesField = typeof(CustomerEditForm).GetField("_notesEdit", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var notesEdit = (DevExpress.XtraEditors.TextEdit)notesField.GetValue(form)!;
        Assert.NotNull(notesEdit);
        Assert.False(notesEdit is DevExpress.XtraEditors.MemoEdit, "Notes editor must be a single-line TextEdit, not MemoEdit");

        // Label 8 (Notes:) must be middle-left aligned for single-line TextEdit
        var label8Field = typeof(CustomerEditForm).GetField("label8", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var label8 = (Label)label8Field.GetValue(form)!;
        Assert.NotNull(label8);
        Assert.Equal(ContentAlignment.MiddleLeft, label8.TextAlign);

        // Editor margins must have deliberate vertical spacing (>= 3px top, >= 4px bottom)
        Assert.True(notesEdit.Margin.Top >= 3, $"Notes margin top was {notesEdit.Margin.Top}");
        Assert.True(notesEdit.Margin.Bottom >= 4, $"Notes margin bottom was {notesEdit.Margin.Bottom}");
    }

    [Fact]
    public void CustomerEditForm_DisablesWindowPlacementPersistence_And_AutoComputeClientSize()
    {
        using var form = new CustomerEditForm("New Customer");

        var autoComputeProp = typeof(CustomerEditForm).GetProperty("AutoComputeClientSize", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(autoComputeProp);
        var autoComputeVal = (bool)autoComputeProp.GetValue(form)!;
        Assert.False(autoComputeVal, "CustomerEditForm must set AutoComputeClientSize=false to maintain explicit dialog bounds");

        var persistProp = typeof(CustomerEditForm).GetProperty("PersistWindowPlacement", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(persistProp);
        var persistVal = (bool)persistProp.GetValue(form)!;
        Assert.False(persistVal, "CustomerEditForm must set PersistWindowPlacement=false to avoid restoring stale oversized windows");
    }
}
