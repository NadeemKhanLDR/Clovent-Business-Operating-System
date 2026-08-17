using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Visual structure of <see cref="PaymentHistoryDialog"/>: a payments grid
/// filling the dialog, with Void Payment/Receipt Preview/Close along the
/// bottom - the same grid/action shape the old <c>PaymentForm</c> modal's
/// history column used, just as its own small standalone dialog now that the
/// tender strip itself is a separate, always-visible, fixed-height control.
/// Data loading/business logic and every handler body live in
/// <c>PaymentHistoryDialog.cs</c>; this file only builds controls, lays them
/// out, and wires each control to its named handler.
/// </summary>
partial class PaymentHistoryDialog
{
    private System.ComponentModel.IContainer components = null;

    private readonly GridControl _paymentsGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _paymentsGridView = new();
    private readonly SimpleButton _voidButton = new() { Text = "Void Payment" };
    private readonly SimpleButton _receiptButton = new() { Text = "Receipt Preview", Name = "ReceiptButton" };
    private readonly SimpleButton _closeButton = new() { Text = "Close", DialogResult = DialogResult.OK };

    /// <summary>Standard Designer-generated disposal of <c>components</c> (<c>PaymentHistoryDialog.Designer.cs</c>) and unsubscription from <c>AppearanceManager.Changed</c>.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        Text = "Payment History";
        MinimumSize = new Size(640, 420);
        Size = new Size(680, 460);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Name = "PaymentHistoryDialog";

        BuildLayout();
        WireEvents();

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += PaymentHistoryDialog_Load;
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

        _paymentsGrid.MainView = _paymentsGridView;
        _paymentsGrid.ViewCollection.Add(_paymentsGridView);
        _paymentsGridView.OptionsBehavior.Editable = false;
        _paymentsGridView.OptionsSelection.MultiSelect = false;
        _paymentsGridView.OptionsView.ShowGroupPanel = false;
        _paymentsGridView.OptionsView.ColumnAutoWidth = true;
        _paymentsGridView.RowHeight = 32;
        _paymentsGridView.Appearance.Row.Font = new Font("Segoe UI", 10.5F);
        _paymentsGridView.Appearance.Row.Options.UseFont = true;
        _paymentsGridView.Columns.AddVisible("MethodName", "Method");
        _paymentsGridView.Columns.AddVisible("Amount", "Amount");
        _paymentsGridView.Columns.AddVisible("Status", "Status");
        _paymentsGridView.Columns.AddVisible("CreatedAtUtc", "Recorded (UTC)");
        _paymentsGridView.CustomColumnDisplayText += (_, e) =>
        {
            if (e.Column.FieldName == "Amount" && e.Value is decimal amount)
            {
                e.DisplayText = CurrencyDisplay.Format(amount);
            }
        };

        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        foreach (var button in new[] { _closeButton, _receiptButton, _voidButton })
        {
            button.AutoSize = true;
            button.MinimumSize = new Size(120, 40);
            button.Margin = new Padding(6, 4, 0, 4);
            actionPanel.Controls.Add(button);
        }

        root.Controls.Add(_paymentsGrid, 0, 0);
        root.Controls.Add(actionPanel, 0, 1);

        Controls.Add(root);
    }

    private void WireEvents()
    {
        _voidButton.Click += async (_, _) => await TryRunAsync(VoidPaymentAsync, "void this payment");
        _receiptButton.Click += async (_, _) => await TryRunAsync(ShowReceiptAsync, "show the receipt preview");
    }
}
