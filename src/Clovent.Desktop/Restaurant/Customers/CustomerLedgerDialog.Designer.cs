using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.Customers;

partial class CustomerLedgerDialog
{
    private System.ComponentModel.IContainer components = null;

    private GridControl _ledgerGrid;
    private GridView _ledgerGridView;
    private SimpleButton _closeButton;

    private LabelControl _outstandingVal;
    private LabelControl _limitVal;
    private LabelControl _availableVal;
    private LabelControl _totalDebitVal;
    private LabelControl _totalCreditVal;

    // Filters and Tools
    private DateEdit _dateFrom;
    private DateEdit _dateTo;
    private ComboBoxEdit _comboType;
    private TextEdit _txtSearchRef;
    private SimpleButton _btnClear;
    private SimpleButton _btnRefresh;
    private SimpleButton _btnPrint;
    private SimpleButton _btnExportPdf;
    private SimpleButton _btnExportExcel;

    // Layout Panels
    private TableLayoutPanel root;
    private TableLayoutPanel summaryPanel;
    private PanelControl cardPanel1;
    private TableLayoutPanel cardLayout1;
    private LabelControl cardTitle1;
    private PanelControl cardPanel2;
    private TableLayoutPanel cardLayout2;
    private LabelControl cardTitle2;
    private PanelControl cardPanel3;
    private TableLayoutPanel cardLayout3;
    private LabelControl cardTitle3;
    private PanelControl cardPanel4;
    private TableLayoutPanel cardLayout4;
    private LabelControl cardTitle4;
    private PanelControl cardPanel5;
    private TableLayoutPanel cardLayout5;
    private LabelControl cardTitle5;
    private TableLayoutPanel filterPanel;
    private FlowLayoutPanel toolsFlow;
    private FlowLayoutPanel actionPanel;

    /// <summary>Clean up any resources being used.</summary>
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
        _ledgerGrid = new GridControl();
        _ledgerGridView = new GridView();
        _closeButton = new SimpleButton();
        _dateFrom = new DateEdit();
        _dateTo = new DateEdit();
        _comboType = new ComboBoxEdit();
        _txtSearchRef = new TextEdit();
        _btnClear = new SimpleButton();
        _btnRefresh = new SimpleButton();
        _btnPrint = new SimpleButton();
        _btnExportPdf = new SimpleButton();
        _btnExportExcel = new SimpleButton();

        root = new TableLayoutPanel();
        summaryPanel = new TableLayoutPanel();
        cardPanel1 = new PanelControl();
        cardLayout1 = new TableLayoutPanel();
        cardTitle1 = new LabelControl();
        _outstandingVal = new LabelControl();
        cardPanel2 = new PanelControl();
        cardLayout2 = new TableLayoutPanel();
        cardTitle2 = new LabelControl();
        _limitVal = new LabelControl();
        cardPanel3 = new PanelControl();
        cardLayout3 = new TableLayoutPanel();
        cardTitle3 = new LabelControl();
        _availableVal = new LabelControl();
        cardPanel4 = new PanelControl();
        cardLayout4 = new TableLayoutPanel();
        cardTitle4 = new LabelControl();
        _totalDebitVal = new LabelControl();
        cardPanel5 = new PanelControl();
        cardLayout5 = new TableLayoutPanel();
        cardTitle5 = new LabelControl();
        _totalCreditVal = new LabelControl();
        filterPanel = new TableLayoutPanel();
        toolsFlow = new FlowLayoutPanel();
        actionPanel = new FlowLayoutPanel();

        ((System.ComponentModel.ISupportInitialize)_ledgerGrid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_ledgerGridView).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_dateFrom.Properties.CalendarTimeProperties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_dateFrom.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_dateTo.Properties.CalendarTimeProperties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_dateTo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_comboType.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtSearchRef.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)cardPanel1).BeginInit();
        cardPanel1.SuspendLayout();
        cardLayout1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)cardPanel2).BeginInit();
        cardPanel2.SuspendLayout();
        cardLayout2.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)cardPanel3).BeginInit();
        cardPanel3.SuspendLayout();
        cardLayout3.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)cardPanel4).BeginInit();
        cardPanel4.SuspendLayout();
        cardLayout4.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)cardPanel5).BeginInit();
        cardPanel5.SuspendLayout();
        cardLayout5.SuspendLayout();
        SuspendLayout();

        Text = "Customer Ledger";
        MinimumSize = new Size(1000, 600);
        Size = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Name = "CustomerLedgerDialog";

        root.Dock = DockStyle.Fill;
        root.ColumnCount = 1;
        root.RowCount = 4;
        root.Padding = new Padding(12);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 85F));  // Summary dashboard
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Filters bar
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // Grid
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));   // Close actions panel

        // --- SUMMARY DASHBOARD ---
        summaryPanel.Dock = DockStyle.Fill;
        summaryPanel.ColumnCount = 5;
        summaryPanel.RowCount = 1;
        summaryPanel.Margin = new Padding(0, 0, 0, 8);
        summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

        // Card 1: Outstanding Balance
        cardPanel1.Dock = DockStyle.Fill;
        cardPanel1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        cardLayout1.Dock = DockStyle.Fill;
        cardLayout1.RowCount = 2;
        cardLayout1.ColumnCount = 1;
        cardLayout1.Padding = new Padding(6);
        cardLayout1.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        cardLayout1.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        cardTitle1.Text = "Outstanding Balance";
        cardTitle1.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        cardTitle1.ForeColor = Color.Gray;
        cardTitle1.Dock = DockStyle.Fill;
        cardTitle1.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _outstandingVal.Text = "Rs. 0.00";
        _outstandingVal.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
        _outstandingVal.Dock = DockStyle.Fill;
        _outstandingVal.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        cardLayout1.Controls.Add(cardTitle1, 0, 0);
        cardLayout1.Controls.Add(_outstandingVal, 0, 1);
        cardPanel1.Controls.Add(cardLayout1);
        summaryPanel.Controls.Add(cardPanel1, 0, 0);

        // Card 2: Credit Limit
        cardPanel2.Dock = DockStyle.Fill;
        cardPanel2.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        cardLayout2.Dock = DockStyle.Fill;
        cardLayout2.RowCount = 2;
        cardLayout2.ColumnCount = 1;
        cardLayout2.Padding = new Padding(6);
        cardLayout2.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        cardLayout2.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        cardTitle2.Text = "Credit Limit";
        cardTitle2.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        cardTitle2.ForeColor = Color.Gray;
        cardTitle2.Dock = DockStyle.Fill;
        cardTitle2.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _limitVal.Text = "Rs. 0.00";
        _limitVal.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
        _limitVal.Dock = DockStyle.Fill;
        _limitVal.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        cardLayout2.Controls.Add(cardTitle2, 0, 0);
        cardLayout2.Controls.Add(_limitVal, 0, 1);
        cardPanel2.Controls.Add(cardLayout2);
        summaryPanel.Controls.Add(cardPanel2, 1, 0);

        // Card 3: Available Credit
        cardPanel3.Dock = DockStyle.Fill;
        cardPanel3.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        cardLayout3.Dock = DockStyle.Fill;
        cardLayout3.RowCount = 2;
        cardLayout3.ColumnCount = 1;
        cardLayout3.Padding = new Padding(6);
        cardLayout3.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        cardLayout3.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        cardTitle3.Text = "Available Credit";
        cardTitle3.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        cardTitle3.ForeColor = Color.Gray;
        cardTitle3.Dock = DockStyle.Fill;
        cardTitle3.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _availableVal.Text = "Rs. 0.00";
        _availableVal.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
        _availableVal.Dock = DockStyle.Fill;
        _availableVal.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        cardLayout3.Controls.Add(cardTitle3, 0, 0);
        cardLayout3.Controls.Add(_availableVal, 0, 1);
        cardPanel3.Controls.Add(cardLayout3);
        summaryPanel.Controls.Add(cardPanel3, 2, 0);

        // Card 4: Total Purchases (Debits)
        cardPanel4.Dock = DockStyle.Fill;
        cardPanel4.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        cardLayout4.Dock = DockStyle.Fill;
        cardLayout4.RowCount = 2;
        cardLayout4.ColumnCount = 1;
        cardLayout4.Padding = new Padding(6);
        cardLayout4.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        cardLayout4.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        cardTitle4.Text = "Total Purchases (Debits)";
        cardTitle4.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        cardTitle4.ForeColor = Color.Gray;
        cardTitle4.Dock = DockStyle.Fill;
        cardTitle4.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _totalDebitVal.Text = "Rs. 0.00";
        _totalDebitVal.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
        _totalDebitVal.Dock = DockStyle.Fill;
        _totalDebitVal.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        cardLayout4.Controls.Add(cardTitle4, 0, 0);
        cardLayout4.Controls.Add(_totalDebitVal, 0, 1);
        cardPanel4.Controls.Add(cardLayout4);
        summaryPanel.Controls.Add(cardPanel4, 3, 0);

        // Card 5: Total Payments (Credits)
        cardPanel5.Dock = DockStyle.Fill;
        cardPanel5.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        cardLayout5.Dock = DockStyle.Fill;
        cardLayout5.RowCount = 2;
        cardLayout5.ColumnCount = 1;
        cardLayout5.Padding = new Padding(6);
        cardLayout5.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        cardLayout5.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        cardTitle5.Text = "Total Payments (Credits)";
        cardTitle5.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        cardTitle5.ForeColor = Color.Gray;
        cardTitle5.Dock = DockStyle.Fill;
        cardTitle5.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _totalCreditVal.Text = "Rs. 0.00";
        _totalCreditVal.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
        _totalCreditVal.Dock = DockStyle.Fill;
        _totalCreditVal.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        cardLayout5.Controls.Add(cardTitle5, 0, 0);
        cardLayout5.Controls.Add(_totalCreditVal, 0, 1);
        cardPanel5.Controls.Add(cardLayout5);
        summaryPanel.Controls.Add(cardPanel5, 4, 0);

        // --- FILTERS BAR ---
        filterPanel.Dock = DockStyle.Fill;
        filterPanel.ColumnCount = 5;
        filterPanel.RowCount = 1;
        filterPanel.Margin = new Padding(0, 0, 0, 4);
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F)); // Date From
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F)); // Date To
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F)); // Type
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F)); // Search Reference
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // Action Buttons

        _dateFrom.Properties.NullValuePrompt = "From Date";
        _dateFrom.Properties.CalendarTimeProperties.Buttons.Clear();
        _dateFrom.Dock = DockStyle.Fill;
        _dateFrom.Font = new Font("Segoe UI", 9F);
        _dateFrom.EditValueChanged += Filter_EditValueChanged;

        _dateTo.Properties.NullValuePrompt = "To Date";
        _dateTo.Properties.CalendarTimeProperties.Buttons.Clear();
        _dateTo.Dock = DockStyle.Fill;
        _dateTo.Font = new Font("Segoe UI", 9F);
        _dateTo.EditValueChanged += Filter_EditValueChanged;

        _comboType.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _comboType.Properties.Items.AddRange(new object[] { "All Transactions", "Sales (Debits)", "Payments (Credits)", "Opening Balance" });
        _comboType.SelectedIndex = 0;
        _comboType.Dock = DockStyle.Fill;
        _comboType.Font = new Font("Segoe UI", 9F);
        _comboType.SelectedIndexChanged += Filter_EditValueChanged;

        _txtSearchRef.Properties.NullValuePrompt = "Ref / Description...";
        _txtSearchRef.Dock = DockStyle.Fill;
        _txtSearchRef.Font = new Font("Segoe UI", 9F);
        _txtSearchRef.EditValueChanged += Filter_EditValueChanged;

        toolsFlow.Dock = DockStyle.Fill;
        toolsFlow.FlowDirection = FlowDirection.LeftToRight;
        toolsFlow.Padding = new Padding(8, 0, 0, 0);

        _btnClear.Text = "Clear";
        _btnClear.Size = new Size(70, 28);
        _btnClear.Font = new Font("Segoe UI", 9F);
        toolsFlow.Controls.Add(_btnClear);

        _btnRefresh.Text = "Refresh";
        _btnRefresh.Size = new Size(80, 28);
        _btnRefresh.Font = new Font("Segoe UI", 9F);
        toolsFlow.Controls.Add(_btnRefresh);

        _btnPrint.Text = "Print";
        _btnPrint.Size = new Size(70, 28);
        _btnPrint.Font = new Font("Segoe UI", 9F);
        toolsFlow.Controls.Add(_btnPrint);

        _btnExportPdf.Text = "PDF";
        _btnExportPdf.Size = new Size(60, 28);
        _btnExportPdf.Font = new Font("Segoe UI", 9F);
        toolsFlow.Controls.Add(_btnExportPdf);

        _btnExportExcel.Text = "Excel";
        _btnExportExcel.Size = new Size(70, 28);
        _btnExportExcel.Font = new Font("Segoe UI", 9F);
        toolsFlow.Controls.Add(_btnExportExcel);

        _btnClear.Click += BtnClear_Click;
        _btnRefresh.Click += BtnRefresh_Click;
        _btnPrint.Click += BtnPrint_Click;
        _btnExportPdf.Click += BtnExportPdf_Click;
        _btnExportExcel.Click += BtnExportExcel_Click;

        filterPanel.Controls.Add(_dateFrom, 0, 0);
        filterPanel.Controls.Add(_dateTo, 1, 0);
        filterPanel.Controls.Add(_comboType, 2, 0);
        filterPanel.Controls.Add(_txtSearchRef, 3, 0);
        filterPanel.Controls.Add(toolsFlow, 4, 0);

        // --- LEDGER GRID ---
        _ledgerGrid.Name = "_ledgerGrid";
        _ledgerGrid.Dock = DockStyle.Fill;
        _ledgerGrid.MainView = _ledgerGridView;
        _ledgerGrid.ViewCollection.Add(_ledgerGridView);
        _ledgerGridView.OptionsBehavior.Editable = false;
        _ledgerGridView.OptionsSelection.MultiSelect = false;
        _ledgerGridView.OptionsView.ShowGroupPanel = false;
        _ledgerGridView.OptionsView.ColumnAutoWidth = true;
        _ledgerGridView.RowHeight = 30;
        _ledgerGridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F);
        _ledgerGridView.Appearance.Row.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colDate = _ledgerGridView.Columns.AddVisible("Date", "Date");
        colDate.Width = 140;
        colDate.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colDate.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colRef = _ledgerGridView.Columns.AddVisible("Reference", "Reference");
        colRef.Width = 120;
        colRef.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colRef.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colDesc = _ledgerGridView.Columns.AddVisible("Description", "Description / Type");
        colDesc.Width = 260;
        colDesc.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colDesc.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colDebit = _ledgerGridView.Columns.AddVisible("Debit", "Debit (Sale)");
        colDebit.Width = 110;
        colDebit.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colDebit.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colCredit = _ledgerGridView.Columns.AddVisible("Credit", "Credit (Payment)");
        colCredit.Width = 110;
        colCredit.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colCredit.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colBal = _ledgerGridView.Columns.AddVisible("RunningBalance", "Running Balance");
        colBal.Width = 120;
        colBal.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colBal.AppearanceHeader.Options.UseFont = true;

        _ledgerGridView.CustomColumnDisplayText += LedgerGridView_CustomColumnDisplayText;

        // --- BUTTONS ---
        actionPanel.Dock = DockStyle.Fill;
        actionPanel.FlowDirection = FlowDirection.RightToLeft;
        _closeButton.Name = "_closeButton";
        _closeButton.Text = "Close";
        _closeButton.DialogResult = DialogResult.OK;
        _closeButton.AutoSize = true;
        _closeButton.MinimumSize = new Size(120, 40);
        actionPanel.Controls.Add(_closeButton);

        // Assemble
        root.Controls.Add(summaryPanel, 0, 0);
        root.Controls.Add(filterPanel, 0, 1);
        root.Controls.Add(_ledgerGrid, 0, 2);
        root.Controls.Add(actionPanel, 0, 3);

        Controls.Add(root);

        ((System.ComponentModel.ISupportInitialize)_ledgerGrid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_ledgerGridView).EndInit();
        ((System.ComponentModel.ISupportInitialize)_dateFrom.Properties.CalendarTimeProperties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_dateFrom.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_dateTo.Properties.CalendarTimeProperties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_dateTo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_comboType.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtSearchRef.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)cardPanel1).EndInit();
        cardPanel1.ResumeLayout(false);
        cardLayout1.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)cardPanel2).EndInit();
        cardPanel2.ResumeLayout(false);
        cardLayout2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)cardPanel3).EndInit();
        cardPanel3.ResumeLayout(false);
        cardLayout3.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)cardPanel4).EndInit();
        cardPanel4.ResumeLayout(false);
        cardLayout4.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)cardPanel5).EndInit();
        cardPanel5.ResumeLayout(false);
        cardLayout5.ResumeLayout(false);

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += CustomerLedgerDialog_Load;

        ResumeLayout(false);
    }
}
