using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.Orders;

partial class RefundOrderDialog
{
    private System.ComponentModel.IContainer components = null;

    private readonly LabelControl _headerLabel = new();
    private readonly LabelControl _subtitleLabel = new();
    private readonly GridControl _linesGrid = new();
    private readonly GridView _linesGridView = new();

    private readonly LabelControl _reasonLabel = new();
    private readonly MemoEdit _reasonEdit = new();

    private readonly LabelControl _settlementMethodLabel = new();
    private readonly ComboBoxEdit _settlementMethodCombo = new();

    private readonly LabelControl _settlementRefLabel = new();
    private readonly TextEdit _settlementRefEdit = new();

    private readonly LabelControl _approverLabel = new();
    private readonly TextEdit _approverEdit = new();

    private readonly LabelControl _summaryLabel = new();
    private readonly SimpleButton _refundRemainingButton = new();
    private readonly SimpleButton _clearQuantitiesButton = new();
    private readonly SimpleButton _submitRefundButton = new();
    private readonly SimpleButton _cancelButton = new();

    /// <summary>Standard Designer-generated disposal of components and unsubscription from events.</summary>
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
        ((System.ComponentModel.ISupportInitialize)_linesGrid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_linesGridView).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_reasonEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_settlementMethodCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_settlementRefEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_approverEdit.Properties).BeginInit();
        SuspendLayout();

        Text = "Process Order Refund";
        MinimumSize = new Size(720, 440);
        Size = new Size(820, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Name = "RefundOrderDialog";

        _headerLabel.Name = "_headerLabel";
        _subtitleLabel.Name = "_subtitleLabel";
        _linesGrid.Name = "_linesGrid";
        _linesGridView.Name = "_linesGridView";
        _reasonLabel.Name = "_reasonLabel";
        _reasonEdit.Name = "_reasonEdit";
        _settlementMethodLabel.Name = "_settlementMethodLabel";
        _settlementMethodCombo.Name = "_settlementMethodCombo";
        _settlementRefLabel.Name = "_settlementRefLabel";
        _settlementRefEdit.Name = "_settlementRefEdit";
        _approverLabel.Name = "_approverLabel";
        _approverEdit.Name = "_approverEdit";
        _summaryLabel.Name = "_summaryLabel";
        _refundRemainingButton.Name = "_refundRemainingButton";
        _clearQuantitiesButton.Name = "_clearQuantitiesButton";
        _submitRefundButton.Name = "_submitRefundButton";
        _cancelButton.Name = "_cancelButton";

        BuildLayout();
        WireEvents();

        ((System.ComponentModel.ISupportInitialize)_linesGrid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_linesGridView).EndInit();
        ((System.ComponentModel.ISupportInitialize)_reasonEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_settlementMethodCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_settlementRefEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_approverEdit.Properties).EndInit();
        ResumeLayout(false);

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += RefundOrderDialog_Load;
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Header
        var headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _headerLabel.Text = "Process Refund";
        _headerLabel.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _headerLabel.Appearance.Options.UseFont = true;
        _headerLabel.Dock = DockStyle.Fill;

        _subtitleLabel.Text = "Select items and quantities to refund. Enter the business reason and settlement method.";
        _subtitleLabel.Appearance.ForeColor = Color.Gray;
        _subtitleLabel.Appearance.Options.UseForeColor = true;
        _subtitleLabel.Dock = DockStyle.Fill;

        headerPanel.Controls.Add(_headerLabel, 0, 0);
        headerPanel.Controls.Add(_subtitleLabel, 0, 1);
        root.Controls.Add(headerPanel, 0, 0);

        // Center Grid
        _linesGrid.Dock = DockStyle.Fill;
        _linesGrid.MainView = _linesGridView;
        _linesGrid.ViewCollection.Add(_linesGridView);
        _linesGridView.OptionsView.ShowGroupPanel = false;
        _linesGridView.OptionsView.ShowIndicator = false;
        _linesGridView.OptionsCustomization.AllowQuickHideColumns = false;
        _linesGridView.RowHeight = 30;
        root.Controls.Add(_linesGrid, 0, 1);

        // Bottom Form & Actions Panel
        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Left fields panel
        var fieldsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            AutoSize = true
        };
        fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        fieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _reasonLabel.Text = "Reason *:";
        _reasonLabel.Padding = new Padding(0, 4, 8, 0);
        _reasonEdit.Dock = DockStyle.Fill;
        _reasonEdit.Height = 44;
        fieldsPanel.Controls.Add(_reasonLabel, 0, 0);
        fieldsPanel.Controls.Add(_reasonEdit, 1, 0);

        _settlementMethodLabel.Text = "Settlement *:";
        _settlementMethodLabel.Padding = new Padding(0, 6, 8, 0);
        _settlementMethodCombo.Dock = DockStyle.Top;
        fieldsPanel.Controls.Add(_settlementMethodLabel, 0, 1);
        fieldsPanel.Controls.Add(_settlementMethodCombo, 1, 1);

        _settlementRefLabel.Text = "Reference:";
        _settlementRefLabel.Padding = new Padding(0, 6, 8, 0);
        _settlementRefEdit.Dock = DockStyle.Top;
        fieldsPanel.Controls.Add(_settlementRefLabel, 0, 2);
        fieldsPanel.Controls.Add(_settlementRefEdit, 1, 2);

        _approverLabel.Text = "Approved By:";
        _approverLabel.Padding = new Padding(0, 6, 8, 0);
        _approverEdit.Dock = DockStyle.Top;
        _approverEdit.Properties.NullValuePrompt = "Verified via Manager Authorization on Submit";
        _approverEdit.Properties.NullValuePromptShowForEmptyValue = true;
        fieldsPanel.Controls.Add(_approverLabel, 0, 3);
        fieldsPanel.Controls.Add(_approverEdit, 1, 3);

        bottomPanel.Controls.Add(fieldsPanel, 0, 0);

        // Right summary & buttons panel
        var actionsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            Padding = new Padding(16, 0, 0, 0)
        };
        actionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _summaryLabel.Text = "Total Refund: 0.00";
        _summaryLabel.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _summaryLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        _summaryLabel.Appearance.Options.UseFont = true;
        _summaryLabel.Appearance.Options.UseTextOptions = true;
        _summaryLabel.Dock = DockStyle.Top;
        actionsPanel.Controls.Add(_summaryLabel, 0, 0);

        TableLayoutPanel quickSelectTable = new TableLayoutPanel();
        quickSelectTable.Dock = DockStyle.Fill;
        quickSelectTable.ColumnCount = 3;
        quickSelectTable.RowCount = 1;
        quickSelectTable.AutoSize = true;
        quickSelectTable.Margin = new Padding(0, 6, 0, 0);
        quickSelectTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        quickSelectTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        quickSelectTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _refundRemainingButton.Text = "Refund Full Remaining";
        _refundRemainingButton.AutoSize = true;
        _refundRemainingButton.MinimumSize = new Size(155, 30);
        _refundRemainingButton.Margin = new Padding(6, 0, 0, 0);

        _clearQuantitiesButton.Text = "Clear Quantities";
        _clearQuantitiesButton.AutoSize = true;
        _clearQuantitiesButton.MinimumSize = new Size(120, 30);
        _clearQuantitiesButton.Margin = new Padding(0, 0, 0, 0);

        quickSelectTable.Controls.Add(_clearQuantitiesButton, 1, 0);
        quickSelectTable.Controls.Add(_refundRemainingButton, 2, 0);
        actionsPanel.Controls.Add(quickSelectTable, 0, 1);

        TableLayoutPanel buttonTable = new TableLayoutPanel();
        buttonTable.Dock = DockStyle.Fill;
        buttonTable.ColumnCount = 3;
        buttonTable.RowCount = 1;
        buttonTable.AutoSize = true;
        buttonTable.Margin = new Padding(0, 8, 0, 0);
        buttonTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        buttonTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _submitRefundButton.Text = "Submit Refund";
        _submitRefundButton.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _submitRefundButton.Appearance.Options.UseFont = true;
        _submitRefundButton.AutoSize = true;
        _submitRefundButton.MinimumSize = new Size(130, 36);
        _submitRefundButton.Margin = new Padding(8, 0, 0, 0);

        _cancelButton.Text = "Cancel";
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.AutoSize = true;
        _cancelButton.MinimumSize = new Size(100, 36);
        _cancelButton.Margin = new Padding(0, 0, 0, 0);

        buttonTable.Controls.Add(_cancelButton, 1, 0);
        buttonTable.Controls.Add(_submitRefundButton, 2, 0);
        actionsPanel.Controls.Add(buttonTable, 0, 2);

        bottomPanel.Controls.Add(actionsPanel, 1, 0);
        root.Controls.Add(bottomPanel, 0, 2);

        Controls.Add(root);
    }
}
