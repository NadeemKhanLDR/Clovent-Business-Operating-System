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

    private PanelControl _headerPanel;
    private LabelControl _titleLabel;
    private LabelControl _customerLabel;
    private LabelControl _subtitleLabel;

    private GridControl _ledgerGrid;
    private GridView _ledgerGridView;
    private SimpleButton _closeButton;

    private LabelControl _outstandingVal;
    private LabelControl _limitVal;
    private LabelControl _availableVal;
    private LabelControl _totalDebitVal;
    private LabelControl _totalCreditVal;

    // Filters and Tools
    private LabelControl _lblPeriod;
    private LabelControl _lblFrom;
    private LabelControl _lblTo;
    private LabelControl _lblType;
    private LabelControl _lblSearch;
    private LabelControl _lblStatus;
    private ComboBoxEdit _periodCombo;
    private DateEdit _dateFrom;
    private DateEdit _dateTo;
    private ComboBoxEdit _comboType;
    private TextEdit _txtSearchRef;
    private SimpleButton _btnLoadLedger;
    private SimpleButton _btnClear;
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
    private PanelControl toolsPanel;
    private FlowLayoutPanel toolsFlowLeft;
    private FlowLayoutPanel toolsFlowRight;
    private PanelControl _statusPanel;
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
        _headerPanel = new PanelControl();
        _titleLabel = new LabelControl();
        _customerLabel = new LabelControl();
        _subtitleLabel = new LabelControl();

        _ledgerGrid = new GridControl();
        _ledgerGridView = new GridView();
        _closeButton = new SimpleButton();

        _lblPeriod = new LabelControl();
        _lblFrom = new LabelControl();
        _lblTo = new LabelControl();
        _lblType = new LabelControl();
        _lblSearch = new LabelControl();
        _lblStatus = new LabelControl();

        _periodCombo = new ComboBoxEdit();
        _dateFrom = new DateEdit();
        _dateTo = new DateEdit();
        _comboType = new ComboBoxEdit();
        _txtSearchRef = new TextEdit();

        _btnLoadLedger = new SimpleButton();
        _btnClear = new SimpleButton();
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
        toolsPanel = new PanelControl();
        toolsFlowLeft = new FlowLayoutPanel();
        toolsFlowRight = new FlowLayoutPanel();
        _statusPanel = new PanelControl();
        actionPanel = new FlowLayoutPanel();

        ((System.ComponentModel.ISupportInitialize)_headerPanel).BeginInit();
        _headerPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_ledgerGrid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_ledgerGridView).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_periodCombo.Properties).BeginInit();
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
        ((System.ComponentModel.ISupportInitialize)toolsPanel).BeginInit();
        toolsPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_statusPanel).BeginInit();
        _statusPanel.SuspendLayout();
        SuspendLayout();

        Text = "Customer Ledger";
        MinimumSize = new Size(1000, 600);
        Size = new Size(1040, 680);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Name = "CustomerLedgerDialog";

        root.Dock = DockStyle.Fill;
        root.ColumnCount = 1;
        root.RowCount = 6;
        root.Padding = new Padding(16, 12, 16, 12);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Row 0: Header
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));        // Row 1: Summary cards
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Row 2: Filters bar
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Row 3: Dedicated status feedback strip
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));        // Row 4: Grid
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));        // Row 5: Close actions panel

        // --- HEADER PANEL ---
        _headerPanel.Dock = DockStyle.Fill;
        _headerPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _headerPanel.Padding = new Padding(0, 0, 0, 8);
        _headerPanel.Margin = new Padding(0);

        var headerFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };

        var titleRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };

        _titleLabel.Text = "CUSTOMER LEDGER STATEMENT";
        _titleLabel.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
        _titleLabel.ForeColor = Color.FromArgb(15, 23, 42);
        _titleLabel.Margin = new Padding(0, 0, 10, 0);

        _customerLabel.Text = string.Empty;
        _customerLabel.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
        _customerLabel.ForeColor = Color.FromArgb(13, 148, 136); // Teal
        _customerLabel.Margin = new Padding(0);

        titleRow.Controls.Add(_titleLabel);
        titleRow.Controls.Add(_customerLabel);

        _subtitleLabel.Text = "Review customer debits, payments and running balance.";
        _subtitleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        _subtitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        _subtitleLabel.Margin = new Padding(0);

        headerFlow.Controls.Add(titleRow);
        headerFlow.Controls.Add(_subtitleLabel);
        _headerPanel.Controls.Add(headerFlow);

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
        cardLayout1.Padding = new Padding(4);
        cardLayout1.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        cardLayout1.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        cardTitle1.Text = "Outstanding Balance";
        cardTitle1.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        cardTitle1.ForeColor = Color.Gray;
        cardTitle1.Dock = DockStyle.Fill;
        cardTitle1.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _outstandingVal.Text = "0.00";
        _outstandingVal.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
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
        cardLayout2.Padding = new Padding(4);
        cardLayout2.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        cardLayout2.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        cardTitle2.Text = "Credit Limit";
        cardTitle2.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        cardTitle2.ForeColor = Color.Gray;
        cardTitle2.Dock = DockStyle.Fill;
        cardTitle2.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _limitVal.Text = "0.00";
        _limitVal.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
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
        cardLayout3.Padding = new Padding(4);
        cardLayout3.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        cardLayout3.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        cardTitle3.Text = "Available Credit";
        cardTitle3.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        cardTitle3.ForeColor = Color.Gray;
        cardTitle3.Dock = DockStyle.Fill;
        cardTitle3.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _availableVal.Text = "0.00";
        _availableVal.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
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
        cardLayout4.Padding = new Padding(4);
        cardLayout4.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        cardLayout4.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        cardTitle4.Text = "Total Purchases";
        cardTitle4.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        cardTitle4.ForeColor = Color.Gray;
        cardTitle4.Dock = DockStyle.Fill;
        cardTitle4.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _totalDebitVal.Text = "0.00";
        _totalDebitVal.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
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
        cardLayout5.Padding = new Padding(4);
        cardLayout5.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        cardLayout5.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        cardTitle5.Text = "Total Payments";
        cardTitle5.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        cardTitle5.ForeColor = Color.Gray;
        cardTitle5.Dock = DockStyle.Fill;
        cardTitle5.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _totalCreditVal.Text = "0.00";
        _totalCreditVal.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _totalCreditVal.Dock = DockStyle.Fill;
        _totalCreditVal.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        cardLayout5.Controls.Add(cardTitle5, 0, 0);
        cardLayout5.Controls.Add(_totalCreditVal, 0, 1);
        cardPanel5.Controls.Add(cardLayout5);
        summaryPanel.Controls.Add(cardPanel5, 4, 0);

        // --- FILTERS BAR ---
        filterPanel.Dock = DockStyle.Fill;
        filterPanel.ColumnCount = 5;
        filterPanel.RowCount = 5;
        filterPanel.AutoSize = true;
        filterPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        filterPanel.Margin = new Padding(0, 0, 0, 4);
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F)); // Period
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F)); // From
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F)); // To
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F)); // Type
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // Remaining space

        filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Row 0: Labels 1
        filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Row 1: Controls 1
        filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Row 2: Search Label
        filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Row 3: Search Input
        filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Row 4: Action Tools

        _lblPeriod.Text = "Report Period";
        _lblPeriod.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _lblPeriod.ForeColor = Color.FromArgb(71, 85, 105);
        _lblPeriod.Dock = DockStyle.Fill;
        _lblPeriod.Margin = new Padding(0, 4, 0, 2);

        _lblFrom.Text = "From";
        _lblFrom.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _lblFrom.ForeColor = Color.FromArgb(71, 85, 105);
        _lblFrom.Dock = DockStyle.Fill;
        _lblFrom.Margin = new Padding(0, 4, 0, 2);

        _lblTo.Text = "To";
        _lblTo.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _lblTo.ForeColor = Color.FromArgb(71, 85, 105);
        _lblTo.Dock = DockStyle.Fill;
        _lblTo.Margin = new Padding(0, 4, 0, 2);

        _lblType.Text = "Transactions";
        _lblType.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _lblType.ForeColor = Color.FromArgb(71, 85, 105);
        _lblType.Dock = DockStyle.Fill;
        _lblType.Margin = new Padding(0, 4, 0, 2);

        _periodCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _periodCombo.Properties.Appearance.Font = new Font("Segoe UI", 9F);
        _periodCombo.Properties.Appearance.Options.UseFont = true;
        _periodCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 9F);
        _periodCombo.Properties.AppearanceDropDown.Options.UseFont = true;
        _periodCombo.Properties.Items.AddRange(new object[] {
            "Today",
            "Yesterday",
            "This Week",
            "Last Week",
            "This Month",
            "Last Month",
            "This Quarter",
            "Last Quarter",
            "This Year",
            "Last Year",
            "Last 7 Days",
            "Last 30 Days",
            "Custom",
            "All Time"});
        _periodCombo.Dock = DockStyle.Fill;
        _periodCombo.SelectedIndexChanged += PeriodCombo_SelectedIndexChanged;

        _dateFrom.Properties.NullValuePrompt = "From Date";
        _dateFrom.Properties.CalendarTimeProperties.Buttons.Clear();
        _dateFrom.Dock = DockStyle.Fill;
        _dateFrom.Font = new Font("Segoe UI", 9F);
        _dateFrom.EditValueChanged += DateEdit_EditValueChanged;

        _dateTo.Properties.NullValuePrompt = "To Date";
        _dateTo.Properties.CalendarTimeProperties.Buttons.Clear();
        _dateTo.Dock = DockStyle.Fill;
        _dateTo.Font = new Font("Segoe UI", 9F);
        _dateTo.EditValueChanged += DateEdit_EditValueChanged;

        _comboType.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _comboType.Properties.Items.AddRange(new object[] { "All Transactions", "Sales (Debits)", "Payments (Credits)", "Opening Balance" });
        _comboType.SelectedIndex = 0;
        _comboType.Dock = DockStyle.Fill;
        _comboType.Font = new Font("Segoe UI", 9F);
        _comboType.SelectedIndexChanged += Filter_EditValueChanged;

        _lblSearch.Text = "Reference / Description";
        _lblSearch.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _lblSearch.ForeColor = Color.FromArgb(71, 85, 105);
        _lblSearch.Dock = DockStyle.Fill;
        _lblSearch.Margin = new Padding(0, 6, 0, 2);

        _txtSearchRef.Properties.NullValuePrompt = "Search reference or description...";
        _txtSearchRef.Dock = DockStyle.Fill;
        _txtSearchRef.Font = new Font("Segoe UI", 9F);
        _txtSearchRef.EditValueChanged += Filter_EditValueChanged;

        // Tools action bar (Load/Clear on left; Print/PDF/Excel on right)
        toolsPanel.Dock = DockStyle.Fill;
        toolsPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        toolsPanel.AutoSize = true;
        toolsPanel.Margin = new Padding(0, 4, 0, 0);

        toolsFlowLeft.Dock = DockStyle.Left;
        toolsFlowLeft.FlowDirection = FlowDirection.LeftToRight;
        toolsFlowLeft.AutoSize = true;
        toolsFlowLeft.WrapContents = false;
        toolsFlowLeft.Margin = new Padding(0);

        toolsFlowRight.Dock = DockStyle.Right;
        toolsFlowRight.FlowDirection = FlowDirection.LeftToRight;
        toolsFlowRight.AutoSize = true;
        toolsFlowRight.WrapContents = false;
        toolsFlowRight.Margin = new Padding(0);

        _btnLoadLedger.Name = "_btnLoadLedger";
        _btnLoadLedger.Text = "Load Ledger";
        _btnLoadLedger.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnLoadLedger.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal primary
        _btnLoadLedger.Appearance.ForeColor = Color.White;
        _btnLoadLedger.Appearance.Options.UseBackColor = true;
        _btnLoadLedger.Appearance.Options.UseForeColor = true;
        _btnLoadLedger.MinimumSize = new Size(110, 32);
        _btnLoadLedger.Margin = new Padding(0, 0, 6, 0);
        _btnLoadLedger.Cursor = Cursors.Hand;
        _btnLoadLedger.Click += BtnLoadLedger_Click;
        toolsFlowLeft.Controls.Add(_btnLoadLedger);

        _btnClear.Name = "_btnClear";
        _btnClear.Text = "Clear";
        _btnClear.Font = new Font("Segoe UI", 9F);
        _btnClear.MinimumSize = new Size(70, 32);
        _btnClear.Margin = new Padding(0, 0, 6, 0);
        _btnClear.Cursor = Cursors.Hand;
        _btnClear.Click += BtnClear_Click;
        toolsFlowLeft.Controls.Add(_btnClear);

        _btnPrint.Name = "_btnPrint";
        _btnPrint.Text = "Print";
        _btnPrint.Font = new Font("Segoe UI", 9F);
        _btnPrint.MinimumSize = new Size(70, 32);
        _btnPrint.Margin = new Padding(0, 0, 6, 0);
        _btnPrint.Cursor = Cursors.Hand;
        _btnPrint.Click += BtnPrint_Click;
        toolsFlowRight.Controls.Add(_btnPrint);

        _btnExportPdf.Name = "_btnExportPdf";
        _btnExportPdf.Text = "PDF";
        _btnExportPdf.Font = new Font("Segoe UI", 9F);
        _btnExportPdf.MinimumSize = new Size(60, 32);
        _btnExportPdf.Margin = new Padding(0, 0, 6, 0);
        _btnExportPdf.Cursor = Cursors.Hand;
        _btnExportPdf.Click += BtnExportPdf_Click;
        toolsFlowRight.Controls.Add(_btnExportPdf);

        _btnExportExcel.Name = "_btnExportExcel";
        _btnExportExcel.Text = "Excel";
        _btnExportExcel.Font = new Font("Segoe UI", 9F);
        _btnExportExcel.MinimumSize = new Size(65, 32);
        _btnExportExcel.Margin = new Padding(0);
        _btnExportExcel.Cursor = Cursors.Hand;
        _btnExportExcel.Click += BtnExportExcel_Click;
        toolsFlowRight.Controls.Add(_btnExportExcel);

        toolsPanel.Controls.Add(toolsFlowLeft);
        toolsPanel.Controls.Add(toolsFlowRight);

        // Row 0: Labels for Top Filters
        filterPanel.Controls.Add(_lblPeriod, 0, 0);
        filterPanel.Controls.Add(_lblFrom, 1, 0);
        filterPanel.Controls.Add(_lblTo, 2, 0);
        filterPanel.Controls.Add(_lblType, 3, 0);

        // Row 1: Controls for Top Filters
        filterPanel.Controls.Add(_periodCombo, 0, 1);
        filterPanel.Controls.Add(_dateFrom, 1, 1);
        filterPanel.Controls.Add(_dateTo, 2, 1);
        filterPanel.Controls.Add(_comboType, 3, 1);

        // Row 2: Search label (full width)
        filterPanel.Controls.Add(_lblSearch, 0, 2);
        filterPanel.SetColumnSpan(_lblSearch, 5);

        // Row 3: Search input (full width)
        filterPanel.Controls.Add(_txtSearchRef, 0, 3);
        filterPanel.SetColumnSpan(_txtSearchRef, 5);

        // Row 4: Action buttons bar (full width)
        filterPanel.Controls.Add(toolsPanel, 0, 4);
        filterPanel.SetColumnSpan(toolsPanel, 5);

        // --- DEDICATED STATUS FEEDBACK STRIP ---
        _statusPanel.Name = "_statusPanel";
        _statusPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _statusPanel.Dock = DockStyle.Fill;
        _statusPanel.AutoSize = true;
        _statusPanel.Margin = new Padding(0, 4, 0, 4);
        _statusPanel.Padding = new Padding(2, 0, 0, 0);

        _lblStatus.Name = "_lblStatus";
        _lblStatus.Text = string.Empty;
        _lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
        _lblStatus.ForeColor = Color.FromArgb(71, 85, 105);
        _lblStatus.Dock = DockStyle.Left;
        _lblStatus.AutoSizeMode = LabelAutoSizeMode.Horizontal;
        _statusPanel.Controls.Add(_lblStatus);

        // --- LEDGER GRID ---
        _ledgerGrid.Name = "_ledgerGrid";
        _ledgerGrid.Dock = DockStyle.Fill;
        _ledgerGrid.MainView = _ledgerGridView;
        _ledgerGrid.ViewCollection.Add(_ledgerGridView);
        _ledgerGridView.OptionsBehavior.Editable = false;
        _ledgerGridView.OptionsSelection.MultiSelect = false;
        _ledgerGridView.OptionsView.ShowGroupPanel = false;
        _ledgerGridView.OptionsView.ColumnAutoWidth = true;
        _ledgerGridView.HorzScrollVisibility = DevExpress.XtraGrid.Views.Base.ScrollVisibility.Auto;
        _ledgerGridView.RowHeight = 30;
        _ledgerGridView.ColumnPanelRowHeight = 32;
        _ledgerGridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F);
        _ledgerGridView.Appearance.Row.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colDate = _ledgerGridView.Columns.AddVisible("Date", "Date");
        colDate.Width = 130;
        colDate.MinWidth = 110;
        colDate.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colDate.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colRef = _ledgerGridView.Columns.AddVisible("Reference", "Reference");
        colRef.Width = 110;
        colRef.MinWidth = 90;
        colRef.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colRef.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colDesc = _ledgerGridView.Columns.AddVisible("Description", "Description / Type");
        colDesc.Width = 250;
        colDesc.MinWidth = 160;
        colDesc.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colDesc.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colDebit = _ledgerGridView.Columns.AddVisible("Debit", "Debit (Sale)");
        colDebit.Width = 110;
        colDebit.MinWidth = 95;
        colDebit.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colDebit.AppearanceHeader.Options.UseFont = true;
        colDebit.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colDebit.AppearanceHeader.Options.UseTextOptions = true;
        colDebit.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colDebit.AppearanceCell.Options.UseTextOptions = true;

        DevExpress.XtraGrid.Columns.GridColumn colCredit = _ledgerGridView.Columns.AddVisible("Credit", "Credit (Payment)");
        colCredit.Width = 110;
        colCredit.MinWidth = 95;
        colCredit.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colCredit.AppearanceHeader.Options.UseFont = true;
        colCredit.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colCredit.AppearanceHeader.Options.UseTextOptions = true;
        colCredit.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colCredit.AppearanceCell.Options.UseTextOptions = true;

        DevExpress.XtraGrid.Columns.GridColumn colBal = _ledgerGridView.Columns.AddVisible("RunningBalance", "Running Balance");
        colBal.Width = 130;
        colBal.MinWidth = 115;
        colBal.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colBal.AppearanceHeader.Options.UseFont = true;
        colBal.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colBal.AppearanceHeader.Options.UseTextOptions = true;
        colBal.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colBal.AppearanceCell.Options.UseTextOptions = true;

        _ledgerGridView.CustomColumnDisplayText += LedgerGridView_CustomColumnDisplayText;
        _ledgerGridView.CustomDrawEmptyForeground += LedgerGridView_CustomDrawEmptyForeground;

        // --- CLOSE BUTTON ---
        actionPanel.Dock = DockStyle.Fill;
        actionPanel.FlowDirection = FlowDirection.RightToLeft;
        _closeButton.Name = "_closeButton";
        _closeButton.Text = "Close";
        _closeButton.DialogResult = DialogResult.OK;
        _closeButton.AutoSize = true;
        _closeButton.MinimumSize = new Size(120, 36);
        actionPanel.Controls.Add(_closeButton);

        // Assemble into root TableLayoutPanel
        root.Controls.Add(_headerPanel, 0, 0);
        root.Controls.Add(summaryPanel, 0, 1);
        root.Controls.Add(filterPanel, 0, 2);
        root.Controls.Add(_statusPanel, 0, 3);
        root.Controls.Add(_ledgerGrid, 0, 4);
        root.Controls.Add(actionPanel, 0, 5);

        Controls.Add(root);

        ((System.ComponentModel.ISupportInitialize)_headerPanel).EndInit();
        _headerPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_ledgerGrid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_ledgerGridView).EndInit();
        ((System.ComponentModel.ISupportInitialize)_periodCombo.Properties).EndInit();
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
        ((System.ComponentModel.ISupportInitialize)toolsPanel).EndInit();
        toolsPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_statusPanel).EndInit();
        _statusPanel.ResumeLayout(false);

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += CustomerLedgerDialog_Load;

        ResumeLayout(false);
    }
}
