using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Orders;
using DevExpress.XtraEditors;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public sealed class DeliveryDetailsDialogLayoutTests
{
    [Fact]
    public void DeliveryDetailsDialog_ColumnWidthAndAddressRow_AreProperlyConstrained()
    {
        using var dialog = new DeliveryDetailsDialog(customerName: "Test");
        dialog.CreateControl();

        var formPanelField = typeof(DeliveryDetailsDialog).GetField("formPanel", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(formPanelField);
        var formPanel = (TableLayoutPanel)formPanelField.GetValue(dialog)!;
        Assert.NotNull(formPanel);

        // Column 0 must be wide enough (>= 140F) so labels do not clip or overlap inputs at 96-240 DPI
        Assert.True(formPanel.ColumnStyles[0].Width >= 140F, $"Label column width is {formPanel.ColumnStyles[0].Width}, expected >= 140F");

        // Address row (Row 3) must NOT be Percent 100F (which would swallow the whole dialog)
        var addressRowStyle = formPanel.RowStyles[3];
        Assert.NotEqual(SizeType.Percent, addressRowStyle.SizeType);
        Assert.True(addressRowStyle.Height <= 120F, $"Address row height is {addressRowStyle.Height}, expected <= 120F");

        // Check controls are properly parented
        var txtAddressField = typeof(DeliveryDetailsDialog).GetField("_txtAddress", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(txtAddressField);
        var txtAddress = (MemoEdit)txtAddressField.GetValue(dialog)!;
        Assert.NotNull(txtAddress.Parent);
        Assert.Equal(ScrollBars.Vertical, txtAddress.Properties.ScrollBars);
    }

    [Fact]
    public void DeliveryDetailsDialog_PrepopulatesCustomerValues()
    {
        using var dialog = new DeliveryDetailsDialog(
            customerName: "Alice Smith",
            customerPhone: "03009876543",
            address: "House 12, Street 4, Sector G",
            source: OrderSource.Online,
            fee: 150.00m,
            riderName: "Rider Bob",
            notes: "Ring bell twice");

        Assert.Equal("Alice Smith", dialog.CustomerName);
        Assert.Equal("03009876543", dialog.CustomerPhone);
        Assert.Equal("House 12, Street 4, Sector G", dialog.DeliveryAddress);
        Assert.Equal(OrderSource.Online, dialog.OrderSource);
        Assert.Equal(150.00m, dialog.DeliveryFee);
        Assert.Equal("Rider Bob", dialog.DeliveryRiderName);
        Assert.Equal("Ring bell twice", dialog.DeliveryNotes);
    }

    [Fact]
    public void DeliveryDetailsDialog_SupportsRiderPhoneAndMemoNotes()
    {
        using var dialog = new DeliveryDetailsDialog(
            customerName: "Charlie Brown",
            customerPhone: "03001234567",
            address: "Street 5",
            riderName: "Dave",
            riderPhone: "03119876543",
            notes: "Please leave package at doorstep");

        Assert.Equal("Dave", dialog.DeliveryRiderName);
        Assert.Equal("03119876543", dialog.DeliveryRiderPhone);
        Assert.Equal("Please leave package at doorstep", dialog.DeliveryNotes);

        var txtNotesField = typeof(DeliveryDetailsDialog).GetField("_txtNotes", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(txtNotesField);
        var txtNotes = (MemoEdit)txtNotesField.GetValue(dialog)!;
        Assert.NotNull(txtNotes);
        Assert.Equal(ScrollBars.Vertical, txtNotes.Properties.ScrollBars);

        Assert.NotNull(dialog.AcceptButton);
        Assert.NotNull(dialog.CancelButton);
    }

    [Fact]
    public void DeliveryDetailsDialog_ExtractsEmbeddedRiderPhone()
    {
        using var dialog = new DeliveryDetailsDialog(
            customerName: "Charlie Brown",
            customerPhone: "03001234567",
            address: "Street 5",
            riderName: "Rider Dave (03215551234)");

        Assert.Equal("Rider Dave", dialog.DeliveryRiderName);
        Assert.Equal("03215551234", dialog.DeliveryRiderPhone);
    }

    [Fact]
    public void DeliveryDetailsDialog_CustomerLookup_PopupConfiguration_ConformsToRequirements()
    {
        using var dialog = new DeliveryDetailsDialog(customerName: "Test");
        dialog.CreateControl();

        var lookupField = typeof(DeliveryDetailsDialog).GetField("_customerLookup", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(lookupField);
        var lookup = (SearchLookUpEdit)lookupField.GetValue(dialog)!;
        Assert.NotNull(lookup);

        // Verification of popup size bounds (500-600 logical px width, 240-300 logical px height)
        var popupSize = lookup.Properties.PopupFormSize;
        Assert.InRange(popupSize.Width, 500, 600);
        Assert.InRange(popupSize.Height, 240, 300);

        Assert.True(lookup.Properties.ShowClearButton);
        Assert.True(lookup.Properties.ShowFooter);

        var popupView = lookup.Properties.PopupView as DevExpress.XtraGrid.Views.Grid.GridView;
        Assert.NotNull(popupView);

        // Best practice grid view options
        Assert.True(popupView.OptionsView.ColumnAutoWidth, "ColumnAutoWidth must be true to avoid horizontal scrollbars");
        Assert.False(popupView.OptionsView.ShowGroupPanel, "ShowGroupPanel must be false");
        Assert.False(popupView.OptionsView.ShowIndicator, "ShowIndicator must be false to save space");
        Assert.False(popupView.OptionsView.RowAutoHeight, "RowAutoHeight must be false");

        // Columns verification: Customer Name, Mobile, Address, Balance
        var colName = popupView.Columns["Name"];
        Assert.NotNull(colName);
        Assert.Equal("Customer Name", colName.Caption);
        Assert.True(colName.Visible);

        var colPhone = popupView.Columns["MobileNumber"];
        Assert.NotNull(colPhone);
        Assert.Equal("Mobile", colPhone.Caption);
        Assert.True(colPhone.Visible);

        var colAddress = popupView.Columns["Address"];
        Assert.NotNull(colAddress);
        Assert.Equal("Address", colAddress.Caption);
        Assert.True(colAddress.Visible);

        var colBalance = popupView.Columns["BalanceDisplay"];
        Assert.NotNull(colBalance);
        Assert.Equal("Balance", colBalance.Caption);
        Assert.True(colBalance.Visible);
        Assert.Equal(DevExpress.Utils.HorzAlignment.Far, colBalance.AppearanceHeader.TextOptions.HAlignment);
        Assert.Equal(DevExpress.Utils.HorzAlignment.Far, colBalance.AppearanceCell.TextOptions.HAlignment);
    }

    [Theory]
    [InlineData(96, 1920, 1080)]
    [InlineData(96, 1366, 768)]
    [InlineData(96, 1024, 768)]
    [InlineData(144, 1920, 1080)]
    [InlineData(192, 1920, 1080)]
    [InlineData(240, 1920, 1080)]
    public void DeliveryDetailsDialog_CalculateClampedPopupBounds_NeverExtendsOutsideWorkingArea(int dpi, int screenWidth, int screenHeight)
    {
        var workingArea = new Rectangle(0, 0, screenWidth, screenHeight - 40); // 40px taskbar
        int desiredW = (int)Math.Round(540 * dpi / 96.0);
        int desiredH = (int)Math.Round(260 * dpi / 96.0);
        var desiredSize = new Size(desiredW, desiredH);

        // Test multiple editor positions: Left, Center, Right-aligned, Near-bottom
        Point[] testEditorLocations =
        [
            new Point(50, 100),
            new Point((screenWidth - 400) / 2, 200),
            new Point(screenWidth - 350, 100),
            new Point(screenWidth - 350, screenHeight - 150)
        ];

        foreach (var loc in testEditorLocations)
        {
            var editorBounds = new Rectangle(loc.X, loc.Y, 300, 30);
            var clamped = DeliveryDetailsDialog.CalculateClampedPopupBounds(desiredSize, editorBounds, workingArea, dpi);

            // Bounds must be completely inside the working area
            Assert.True(clamped.Left >= workingArea.Left, $"Left {clamped.Left} was < {workingArea.Left} at dpi {dpi}, res {screenWidth}x{screenHeight}");
            Assert.True(clamped.Right <= workingArea.Right, $"Right {clamped.Right} was > {workingArea.Right} at dpi {dpi}, res {screenWidth}x{screenHeight}");
            Assert.True(clamped.Top >= workingArea.Top, $"Top {clamped.Top} was < {workingArea.Top} at dpi {dpi}, res {screenWidth}x{screenHeight}");
            Assert.True(clamped.Bottom <= workingArea.Bottom, $"Bottom {clamped.Bottom} was > {workingArea.Bottom} at dpi {dpi}, res {screenWidth}x{screenHeight}");
        }
    }

    [Fact]
    public void DeliveryDetailsDialog_SelectingCustomer_ImmediatelyFillsCustomerFields_AndAllowsEditing()
    {
        using var dialog = new DeliveryDetailsDialog(customerName: "Test");
        dialog.CreateControl();

        var customersField = typeof(DeliveryDetailsDialog).GetField("_customers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var custId = Guid.NewGuid();
        var demoCustomer = new Clovent.Restaurant.Application.Customers.Dtos.CustomerDto(
            CustomerId: custId,
            Code: "CREDIT-DEMO",
            Name: "Corporate Account Demo",
            MobileNumber: "0300-1234567",
            Address: "Corporate Plaza, F-7, Islamabad",
            Email: "demo@corporate.com",
            OpeningBalance: 0m,
            CreditLimit: 25000.00m,
            OutstandingBalance: 7100.00m,
            IsActive: true,
            Notes: null,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            IsCreditAllowed: true,
            IsDefault: false);

        customersField.SetValue(dialog, new System.Collections.Generic.List<Clovent.Restaurant.Application.Customers.Dtos.CustomerDto> { demoCustomer });

        var lookupField = typeof(DeliveryDetailsDialog).GetField("_customerLookup", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var lookup = (SearchLookUpEdit)lookupField.GetValue(dialog)!;

        // Simulate customer selection
        lookup.EditValue = custId;

        // Verification: fields immediately populated
        Assert.Equal("Corporate Account Demo", dialog.CustomerName);
        Assert.Equal("0300-1234567", dialog.CustomerPhone);
        Assert.Equal("Corporate Plaza, F-7, Islamabad", dialog.DeliveryAddress);
        Assert.Equal(custId, dialog.SelectedCustomerId);

        // Verification: Cashier can edit specific delivery destination address without wiping customer ID
        var txtAddressField = typeof(DeliveryDetailsDialog).GetField("_txtAddress", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var txtAddress = (MemoEdit)txtAddressField.GetValue(dialog)!;
        txtAddress.Text = "Corporate Plaza, Floor 4, Suite 402, F-7, Islamabad";

        Assert.Equal("Corporate Plaza, Floor 4, Suite 402, F-7, Islamabad", dialog.DeliveryAddress);
        Assert.Equal(custId, dialog.SelectedCustomerId);
    }

    [Fact]
    public void DeliveryDetailsDialog_SpecialNotesAndAddress_SupportMultilineText()
    {
        using var dialog = new DeliveryDetailsDialog(customerName: "Test");
        dialog.CreateControl();

        var txtNotesField = typeof(DeliveryDetailsDialog).GetField("_txtNotes", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var txtNotes = (MemoEdit)txtNotesField.GetValue(dialog)!;

        string multilineNote = "Deliver to back gate.\r\nCall on arrival.\r\nRing buzzer 12.";
        txtNotes.Text = multilineNote;

        Assert.Equal(multilineNote, dialog.DeliveryNotes);
        Assert.Contains("\n", dialog.DeliveryNotes);
    }

    [Fact]
    public void DeliveryDetailsDialog_LayoutAndSpacing_AreContentDrivenWithoutSpacerRow()
    {
        using var dialog = new DeliveryDetailsDialog(customerName: "Test");
        dialog.CreateControl();

        var rootField = typeof(DeliveryDetailsDialog).GetField("root", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var root = (TableLayoutPanel)rootField.GetValue(dialog)!;
        Assert.NotNull(root);

        // Root row 1 (formPanel) must be SizeType.Percent 100F so it never collapses to 36px!
        Assert.Equal(SizeType.AutoSize, root.RowStyles[0].SizeType);
        Assert.Equal(SizeType.Percent, root.RowStyles[1].SizeType);
        Assert.Equal(SizeType.AutoSize, root.RowStyles[2].SizeType);

        var formPanelField = typeof(DeliveryDetailsDialog).GetField("formPanel", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var formPanel = (TableLayoutPanel)formPanelField.GetValue(dialog)!;
        Assert.NotNull(formPanel);

        // Exactly 9 rows, no extra spacer row
        Assert.Equal(9, formPanel.RowCount);
        Assert.True(formPanel.AutoScroll, "formPanel must enable AutoScroll for DPI safety");

        // Row 8 is Special Notes, which must have comfortable multiline height (>= 50)
        var notesRowStyle = formPanel.RowStyles[8];
        Assert.True(notesRowStyle.Height >= 50F, $"Special Notes row height is {notesRowStyle.Height}, expected >= 50F");

        // Subheader label must contain full text and not be clipped
        var subheaderField = typeof(DeliveryDetailsDialog).GetField("subheaderLabel", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var subheader = (LabelControl)subheaderField.GetValue(dialog)!;
        Assert.NotNull(subheader);
        Assert.Contains("rider details", subheader.Text);

        // Dialog client height should be compact and content-driven (around 455, not bloated)
        Assert.True(dialog.ClientSize.Height <= 500, $"Dialog client height {dialog.ClientSize.Height} should be <= 500");
    }

    [Fact]
    public void DeliveryDetailsDialog_AllNineFields_AreVisible_Parented_AndInCorrectVerticalOrder()
    {
        using var dialog = new DeliveryDetailsDialog(customerName: "Test Customer");
        dialog.CreateControl();

        var formPanel = (TableLayoutPanel)typeof(DeliveryDetailsDialog)
            .GetField("formPanel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(dialog)!;

        string[] controlFields =
        [
            "_customerPickerPanel",
            "_txtName",
            "_txtPhone",
            "_txtAddress",
            "_comboSource",
            "_spinDeliveryFee",
            "_txtRider",
            "_txtRiderPhone",
            "_txtNotes"
        ];

        int lastTop = -1;
        foreach (var fieldName in controlFields)
        {
            var field = typeof(DeliveryDetailsDialog).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);

            var ctrl = (Control)field.GetValue(dialog)!;
            Assert.NotNull(ctrl);
            Assert.NotNull(ctrl.Parent);
            Assert.True(ctrl.Width > 0, $"Control {fieldName} has width <= 0");
            Assert.True(ctrl.Height > 0, $"Control {fieldName} has height <= 0");

            // Control must be inside formPanel
            Assert.True(ctrl.Parent == formPanel, $"Control {fieldName} parent is not formPanel");

            // Check vertical ordering
            Assert.True(ctrl.Top > lastTop, $"Control {fieldName} at Top={ctrl.Top} is not below previous control at Top={lastTop}");
            lastTop = ctrl.Top;
        }

        // Verify bottom action buttons
        var btnConfirm = (Control)typeof(DeliveryDetailsDialog)
            .GetField("_btnConfirm", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(dialog)!;
        var btnCancel = (Control)typeof(DeliveryDetailsDialog)
            .GetField("_btnCancel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(dialog)!;

        Assert.NotNull(btnConfirm.Parent);
        Assert.NotNull(btnCancel.Parent);
        Assert.True(btnConfirm.Width > 0);
        Assert.True(btnCancel.Width > 0);
    }
}
