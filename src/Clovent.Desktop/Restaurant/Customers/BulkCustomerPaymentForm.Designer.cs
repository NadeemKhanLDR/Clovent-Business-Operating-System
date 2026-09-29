using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.Customers;

partial class BulkCustomerPaymentForm
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel root;
    private TableLayoutPanel topPanel;
    private TableLayoutPanel configPanel;
    private FlowLayoutPanel quickActionsPanel;
    private TableLayoutPanel summaryPanel;
    private FlowLayoutPanel actionPanel;

    private LabelControl lblHeader;
    private LabelControl lblSubtitle;
    private LabelControl _lblSectionPaymentDetails;
    private LabelControl _lblPaymentMethod;
    private LabelControl _lblBatchReference;
    private LabelControl _lblBatchNotes;

    private ComboBoxEdit _comboPaymentMethod;
    private TextEdit _txtBatchReference;
    private TextEdit _txtNotes;
    private SimpleButton _btnSelectAll;
    private SimpleButton _btnClearSelection;
    private SimpleButton _btnFillOutstanding;

    private GridControl _gridControl;
    private GridView _gridView;

    private LabelControl _lblSelectedCount;
    private LabelControl _lblTotalAmount;
    private LabelControl _lblTotalApplied;
    private LabelControl _lblTotalAdvance;

    private SimpleButton _btnCancel;
    private SimpleButton _btnSubmit;

    /// <summary>Clean up any resources being used.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        root = new TableLayoutPanel();
        topPanel = new TableLayoutPanel();
        configPanel = new TableLayoutPanel();
        quickActionsPanel = new FlowLayoutPanel();
        summaryPanel = new TableLayoutPanel();
        actionPanel = new FlowLayoutPanel();

        lblHeader = new LabelControl();
        lblSubtitle = new LabelControl();
        _lblSectionPaymentDetails = new LabelControl();
        _lblPaymentMethod = new LabelControl();
        _lblBatchReference = new LabelControl();
        _lblBatchNotes = new LabelControl();

        _comboPaymentMethod = new ComboBoxEdit();
        _txtBatchReference = new TextEdit();
        _txtNotes = new TextEdit();
        _btnSelectAll = new SimpleButton();
        _btnClearSelection = new SimpleButton();
        _btnFillOutstanding = new SimpleButton();

        _gridControl = new GridControl();
        _gridView = new GridView();

        _lblSelectedCount = new LabelControl();
        _lblTotalAmount = new LabelControl();
        _lblTotalApplied = new LabelControl();
        _lblTotalAdvance = new LabelControl();

        _btnCancel = new SimpleButton();
        _btnSubmit = new SimpleButton();

        ((System.ComponentModel.ISupportInitialize)_comboPaymentMethod.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtBatchReference.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtNotes.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gridControl).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).BeginInit();
        root.SuspendLayout();
        topPanel.SuspendLayout();
        configPanel.SuspendLayout();
        quickActionsPanel.SuspendLayout();
        summaryPanel.SuspendLayout();
        actionPanel.SuspendLayout();
        SuspendLayout();

        // root
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(topPanel, 0, 0);
        root.Controls.Add(configPanel, 0, 1);
        root.Controls.Add(quickActionsPanel, 0, 2);
        root.Controls.Add(_gridControl, 0, 3);
        root.Controls.Add(summaryPanel, 0, 4);
        root.Controls.Add(actionPanel, 0, 5);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Name = "root";
        root.RowCount = 6;
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Size = new Size(1060, 680);
        root.TabIndex = 0;

        // topPanel (Row 0)
        topPanel.AutoSize = true;
        topPanel.ColumnCount = 1;
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        topPanel.Controls.Add(lblHeader, 0, 0);
        topPanel.Controls.Add(lblSubtitle, 0, 1);
        topPanel.Dock = DockStyle.Fill;
        topPanel.Location = new Point(14, 10);
        topPanel.Margin = new Padding(14, 10, 14, 4);
        topPanel.Name = "topPanel";
        topPanel.RowCount = 2;
        topPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topPanel.TabIndex = 0;

        lblHeader.Appearance.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblHeader.Appearance.Options.UseFont = true;
        lblHeader.Dock = DockStyle.Fill;
        lblHeader.Text = "BULK CUSTOMER COLLECTIONS";
        lblHeader.Margin = new Padding(0, 0, 0, 2);

        lblSubtitle.Appearance.Font = new Font("Segoe UI", 9F);
        lblSubtitle.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        lblSubtitle.Appearance.Options.UseFont = true;
        lblSubtitle.Appearance.Options.UseForeColor = true;
        lblSubtitle.Dock = DockStyle.Fill;
        lblSubtitle.Text = "Collect payments across multiple customer accounts in a single atomic transaction. Cash collections require an active cashier shift.";

        // configPanel (Row 1 - Batch Configuration)
        configPanel.AutoSize = true;
        configPanel.ColumnCount = 3;
        configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
        configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
        configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        configPanel.Dock = DockStyle.Fill;
        configPanel.Location = new Point(14, 60);
        configPanel.Margin = new Padding(14, 4, 14, 4);
        configPanel.Name = "configPanel";
        configPanel.RowCount = 3;
        configPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        configPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        configPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        configPanel.TabIndex = 1;

        // Row 0: Section label
        _lblSectionPaymentDetails.Text = "PAYMENT DETAILS";
        _lblSectionPaymentDetails.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblSectionPaymentDetails.ForeColor = Color.FromArgb(14, 116, 144);
        _lblSectionPaymentDetails.Dock = DockStyle.Fill;
        _lblSectionPaymentDetails.Margin = new Padding(0, 4, 0, 4);
        configPanel.Controls.Add(_lblSectionPaymentDetails, 0, 0);
        configPanel.SetColumnSpan(_lblSectionPaymentDetails, 3);

        // Row 1: Labels
        _lblPaymentMethod.Text = "Payment Method *";
        _lblPaymentMethod.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _lblPaymentMethod.ForeColor = Color.FromArgb(71, 85, 105);
        _lblPaymentMethod.Dock = DockStyle.Fill;
        _lblPaymentMethod.Margin = new Padding(0, 2, 8, 2);

        _lblBatchReference.Text = "Batch Reference / Cheque #";
        _lblBatchReference.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _lblBatchReference.ForeColor = Color.FromArgb(71, 85, 105);
        _lblBatchReference.Dock = DockStyle.Fill;
        _lblBatchReference.Margin = new Padding(0, 2, 8, 2);

        _lblBatchNotes.Text = "Batch Notes / Memo";
        _lblBatchNotes.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _lblBatchNotes.ForeColor = Color.FromArgb(71, 85, 105);
        _lblBatchNotes.Dock = DockStyle.Fill;
        _lblBatchNotes.Margin = new Padding(0, 2, 0, 2);

        configPanel.Controls.Add(_lblPaymentMethod, 0, 1);
        configPanel.Controls.Add(_lblBatchReference, 1, 1);
        configPanel.Controls.Add(_lblBatchNotes, 2, 1);

        // Row 2: Editors
        _comboPaymentMethod.Dock = DockStyle.Fill;
        _comboPaymentMethod.Margin = new Padding(0, 2, 8, 4);
        _comboPaymentMethod.MinimumSize = new Size(0, 28);
        _comboPaymentMethod.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _comboPaymentMethod.Properties.Appearance.Options.UseFont = true;
        _comboPaymentMethod.Properties.NullText = "Select Payment Method...";
        _comboPaymentMethod.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

        _txtBatchReference.Dock = DockStyle.Fill;
        _txtBatchReference.Margin = new Padding(0, 2, 8, 4);
        _txtBatchReference.MinimumSize = new Size(0, 28);
        _txtBatchReference.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtBatchReference.Properties.Appearance.Options.UseFont = true;
        _txtBatchReference.Properties.NullValuePrompt = "Batch Reference / Cheque #";

        _txtNotes.Dock = DockStyle.Fill;
        _txtNotes.Margin = new Padding(0, 2, 0, 4);
        _txtNotes.MinimumSize = new Size(0, 28);
        _txtNotes.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtNotes.Properties.Appearance.Options.UseFont = true;
        _txtNotes.Properties.NullValuePrompt = "Batch Notes / Memo...";

        configPanel.Controls.Add(_comboPaymentMethod, 0, 2);
        configPanel.Controls.Add(_txtBatchReference, 1, 2);
        configPanel.Controls.Add(_txtNotes, 2, 2);

        // quickActionsPanel (Row 2 - Quick Action Buttons)
        quickActionsPanel.AutoSize = true;
        quickActionsPanel.Controls.Add(_btnSelectAll);
        quickActionsPanel.Controls.Add(_btnClearSelection);
        quickActionsPanel.Controls.Add(_btnFillOutstanding);
        quickActionsPanel.Dock = DockStyle.Fill;
        quickActionsPanel.Location = new Point(14, 102);
        quickActionsPanel.Margin = new Padding(14, 2, 14, 6);
        quickActionsPanel.Name = "quickActionsPanel";
        quickActionsPanel.Padding = new Padding(0, 2, 0, 2);
        quickActionsPanel.Size = new Size(1032, 38);
        quickActionsPanel.TabIndex = 2;

        _btnSelectAll.Appearance.Font = new Font("Segoe UI", 9F);
        _btnSelectAll.Appearance.Options.UseFont = true;
        _btnSelectAll.Margin = new Padding(0, 2, 8, 2);
        _btnSelectAll.MinimumSize = new Size(90, 30);
        _btnSelectAll.Size = new Size(90, 30);
        _btnSelectAll.Text = "Select All";

        _btnClearSelection.Appearance.Font = new Font("Segoe UI", 9F);
        _btnClearSelection.Appearance.Options.UseFont = true;
        _btnClearSelection.Margin = new Padding(0, 2, 8, 2);
        _btnClearSelection.MinimumSize = new Size(90, 30);
        _btnClearSelection.Size = new Size(90, 30);
        _btnClearSelection.Text = "Clear All";

        _btnFillOutstanding.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnFillOutstanding.Appearance.Options.UseFont = true;
        _btnFillOutstanding.Margin = new Padding(0, 2, 8, 2);
        _btnFillOutstanding.MinimumSize = new Size(200, 30);
        _btnFillOutstanding.Size = new Size(210, 30);
        _btnFillOutstanding.Text = "Fill Selected with Outstanding";

        // _gridControl (Row 3 - Grid)
        _gridControl.Dock = DockStyle.Fill;
        _gridControl.Location = new Point(14, 148);
        _gridControl.MainView = _gridView;
        _gridControl.Margin = new Padding(14, 2, 14, 4);
        _gridControl.Name = "_gridControl";
        _gridControl.Size = new Size(1032, 410);
        _gridControl.TabIndex = 3;
        _gridControl.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { _gridView });

        // _gridView
        _gridView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _gridView.Appearance.HeaderPanel.Options.UseFont = true;
        _gridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F);
        _gridView.Appearance.Row.Options.UseFont = true;
        _gridView.GridControl = _gridControl;
        _gridView.Name = "_gridView";
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ShowIndicator = false;
        _gridView.RowHeight = 32;

        // summaryPanel (Row 4 - Totals & Summary KPI Cards)
        summaryPanel.AutoSize = true;
        summaryPanel.ColumnCount = 4;
        summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        summaryPanel.Controls.Add(_lblSelectedCount, 0, 0);
        summaryPanel.Controls.Add(_lblTotalAmount, 1, 0);
        summaryPanel.Controls.Add(_lblTotalApplied, 2, 0);
        summaryPanel.Controls.Add(_lblTotalAdvance, 3, 0);
        summaryPanel.Dock = DockStyle.Fill;
        summaryPanel.Location = new Point(14, 564);
        summaryPanel.Margin = new Padding(14, 4, 14, 6);
        summaryPanel.MinimumSize = new Size(0, 42);
        summaryPanel.Name = "summaryPanel";
        summaryPanel.RowCount = 1;
        summaryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        summaryPanel.TabIndex = 4;

        void ConfigureSummaryLabel(LabelControl lbl, string prefix, Color color)
        {
            lbl.Appearance.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lbl.Appearance.ForeColor = color;
            lbl.Appearance.Options.UseFont = true;
            lbl.Appearance.Options.UseForeColor = true;
            lbl.Appearance.Options.UseTextOptions = true;
            lbl.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lbl.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            lbl.Dock = DockStyle.Fill;
            lbl.Text = prefix;
        }

        ConfigureSummaryLabel(_lblSelectedCount, "Selected: 0", Color.FromArgb(30, 41, 59));
        ConfigureSummaryLabel(_lblTotalAmount, "Total Payment: 0.00", Color.FromArgb(13, 148, 136));
        ConfigureSummaryLabel(_lblTotalApplied, "Applied to A/R: 0.00", Color.FromArgb(22, 101, 52));
        ConfigureSummaryLabel(_lblTotalAdvance, "New Advances: 0.00", Color.FromArgb(30, 64, 175));

        // actionPanel (Row 5 - Footer Actions)
        actionPanel.AutoSize = true;
        actionPanel.Controls.Add(_btnCancel);
        actionPanel.Controls.Add(_btnSubmit);
        actionPanel.Dock = DockStyle.Fill;
        actionPanel.FlowDirection = FlowDirection.RightToLeft;
        actionPanel.Location = new Point(14, 616);
        actionPanel.Margin = new Padding(14, 4, 14, 10);
        actionPanel.Name = "actionPanel";
        actionPanel.Size = new Size(1032, 46);
        actionPanel.TabIndex = 5;

        _btnCancel.Appearance.Font = new Font("Segoe UI", 9.5F);
        _btnCancel.Appearance.Options.UseFont = true;
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Margin = new Padding(8, 4, 0, 4);
        _btnCancel.MinimumSize = new Size(100, 36);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new Size(100, 36);
        _btnCancel.TabIndex = 0;
        _btnCancel.Text = "Cancel";

        _btnSubmit.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _btnSubmit.Appearance.Options.UseFont = true;
        _btnSubmit.Margin = new Padding(8, 4, 0, 4);
        _btnSubmit.MinimumSize = new Size(190, 36);
        _btnSubmit.Name = "_btnSubmit";
        _btnSubmit.Size = new Size(190, 36);
        _btnSubmit.TabIndex = 1;
        _btnSubmit.Text = "Record Bulk Collection";

        // Form
        AcceptButton = _btnSubmit;
        CancelButton = _btnCancel;
        ClientSize = new Size(1060, 680);
        Controls.Add(root);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        Name = "BulkCustomerPaymentForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Bulk Customer Collections";

        ((System.ComponentModel.ISupportInitialize)_comboPaymentMethod.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtBatchReference.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtNotes.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridControl).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).EndInit();
        root.ResumeLayout(false);
        root.PerformLayout();
        topPanel.ResumeLayout(false);
        topPanel.PerformLayout();
        configPanel.ResumeLayout(false);
        quickActionsPanel.ResumeLayout(false);
        quickActionsPanel.PerformLayout();
        summaryPanel.ResumeLayout(false);
        actionPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
