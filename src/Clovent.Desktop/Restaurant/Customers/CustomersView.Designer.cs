using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.Customers;

partial class CustomersView
{
    private System.ComponentModel.IContainer components = null;

    private GridControl _gridControl;
    private GridView _gridView;
    private TextEdit _txtSearch;
    private ComboBoxEdit _comboStatus;
    private SimpleButton _btnClearFilters;

    private SimpleButton _newButton;
    private SimpleButton _refreshButton;
    private SimpleButton _exportButton;
    private SimpleButton _btnReceivePayment;
    private SimpleButton _btnLedger;
    private SimpleButton _btnToggleStatus;

    private LabelControl _lblTotalCustomers;
    private LabelControl _lblActiveCustomers;
    private LabelControl _lblWithBalance;
    private LabelControl _lblTotalOutstanding;

    // Layout Panels and Static Labels
    private TableLayoutPanel root;
    private TableLayoutPanel topPanel;
    private LabelControl headerLabel;
    private FlowLayoutPanel actionsPanel;
    private TableLayoutPanel filterPanel;
    private TableLayoutPanel bottomPanel;

    private void InitializeComponent()
    {
        _gridControl = new GridControl();
        _gridView = new GridView();
        _txtSearch = new TextEdit();
        _comboStatus = new ComboBoxEdit();
        _btnClearFilters = new SimpleButton();

        _newButton = new SimpleButton();
        _refreshButton = new SimpleButton();
        _exportButton = new SimpleButton();
        _btnReceivePayment = new SimpleButton();
        _btnLedger = new SimpleButton();
        _btnToggleStatus = new SimpleButton();

        _lblTotalCustomers = new LabelControl();
        _lblActiveCustomers = new LabelControl();
        _lblWithBalance = new LabelControl();
        _lblTotalOutstanding = new LabelControl();

        root = new TableLayoutPanel();
        topPanel = new TableLayoutPanel();
        headerLabel = new LabelControl();
        actionsPanel = new FlowLayoutPanel();
        filterPanel = new TableLayoutPanel();
        bottomPanel = new TableLayoutPanel();

        ((System.ComponentModel.ISupportInitialize)_gridControl).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtSearch.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_comboStatus.Properties).BeginInit();
        SuspendLayout();

        Dock = DockStyle.Fill;
        Name = "CustomersView";

        // Root Layout
        root.Dock = DockStyle.Fill;
        root.ColumnCount = 1;
        root.RowCount = 4;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));  // Header & Buttons
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Filter row
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Grid
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Bottom Summary

        // --- TOP HEADER & BUTTONS ---
        topPanel.Dock = DockStyle.Fill;
        topPanel.ColumnCount = 2;
        topPanel.RowCount = 1;
        topPanel.Padding = new Padding(12, 6, 12, 6);
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        headerLabel.Text = "Customers";
        headerLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        headerLabel.Dock = DockStyle.Fill;
        headerLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

        actionsPanel.Dock = DockStyle.Fill;
        actionsPanel.FlowDirection = FlowDirection.RightToLeft;
        actionsPanel.Padding = new Padding(0, 2, 0, 0);

        _btnLedger.Text = "View Ledger";
        _btnLedger.MinimumSize = new Size(110, 32);
        _btnLedger.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_btnLedger);

        _btnReceivePayment.Text = "Receive Payment";
        _btnReceivePayment.MinimumSize = new Size(130, 32);
        _btnReceivePayment.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_btnReceivePayment);

        _btnToggleStatus.Text = "Deactivate";
        _btnToggleStatus.MinimumSize = new Size(100, 32);
        _btnToggleStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_btnToggleStatus);

        _exportButton.Text = "Export CSV";
        _exportButton.MinimumSize = new Size(95, 32);
        _exportButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_exportButton);

        _refreshButton.Text = "Refresh";
        _refreshButton.MinimumSize = new Size(80, 32);
        _refreshButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_refreshButton);

        _newButton.Text = "+ New Customer";
        _newButton.MinimumSize = new Size(130, 32);
        _newButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_newButton);

        topPanel.Controls.Add(headerLabel, 0, 0);
        topPanel.Controls.Add(actionsPanel, 1, 0);

        // --- FILTER BAR ---
        filterPanel.Dock = DockStyle.Fill;
        filterPanel.ColumnCount = 4;
        filterPanel.RowCount = 1;
        filterPanel.Padding = new Padding(12, 4, 12, 4);
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F)); // Search box
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F)); // Status combo
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F)); // Clear Filters
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        _txtSearch.Properties.NullValuePrompt = "Search Customer...";
        _txtSearch.Dock = DockStyle.Fill;
        _txtSearch.Font = new Font("Segoe UI", 9.5F);
        _txtSearch.EditValueChanged += TxtSearch_EditValueChanged;

        _comboStatus.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _comboStatus.Properties.Items.AddRange(new object[] { "All", "Active", "Inactive" });
        _comboStatus.SelectedIndex = 0;
        _comboStatus.Dock = DockStyle.Fill;
        _comboStatus.Font = new Font("Segoe UI", 9.5F);
        _comboStatus.SelectedIndexChanged += ComboStatus_SelectedIndexChanged;

        _btnClearFilters.Text = "Clear Filters";
        _btnClearFilters.Dock = DockStyle.Fill;
        _btnClearFilters.Font = new Font("Segoe UI", 9F);
        _btnClearFilters.Click += BtnClearFilters_Click;

        filterPanel.Controls.Add(_txtSearch, 0, 0);
        filterPanel.Controls.Add(_comboStatus, 1, 0);
        filterPanel.Controls.Add(_btnClearFilters, 2, 0);

        // --- MAIN GRID ---
        _gridControl.Dock = DockStyle.Fill;
        _gridControl.MainView = _gridView;
        _gridControl.ViewCollection.Add(_gridView);

        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsSelection.MultiSelect = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ColumnAutoWidth = true;
        _gridView.RowHeight = 32;
        _gridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F);
        _gridView.Appearance.Row.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colCode = _gridView.Columns.AddVisible("Code", "Code");
        colCode.Width = 100;
        colCode.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colCode.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colName = _gridView.Columns.AddVisible("Name", "Customer Name");
        colName.Width = 180;
        colName.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colName.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colMobile = _gridView.Columns.AddVisible("MobileNumber", "Mobile");
        colMobile.Width = 120;
        colMobile.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colMobile.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colEmail = _gridView.Columns.AddVisible("Email", "Email");
        colEmail.Width = 160;
        colEmail.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colEmail.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colOutstanding = _gridView.Columns.AddVisible("OutstandingBalance", "Outstanding");
        colOutstanding.Width = 120;
        colOutstanding.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colOutstanding.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colLimit = _gridView.Columns.AddVisible("CreditLimit", "Credit Limit");
        colLimit.Width = 120;
        colLimit.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colLimit.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colStatus = _gridView.Columns.AddVisible("StatusText", "Status");
        colStatus.Width = 90;
        colStatus.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colStatus.AppearanceHeader.Options.UseFont = true;

        DevExpress.XtraGrid.Columns.GridColumn colLastTx = _gridView.Columns.AddVisible("LastTransactionText", "Last Transaction");
        colLastTx.Width = 140;
        colLastTx.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colLastTx.AppearanceHeader.Options.UseFont = true;

        _gridView.CustomColumnDisplayText += GridView_CustomColumnDisplayText;
        _gridView.FocusedRowChanged += GridView_FocusedRowChanged;
        _gridView.RowCellClick += GridView_RowCellClick;

        // --- BOTTOM SUMMARY ---
        bottomPanel.Dock = DockStyle.Fill;
        bottomPanel.ColumnCount = 4;
        bottomPanel.RowCount = 1;
        bottomPanel.Padding = new Padding(12, 6, 12, 6);
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        _lblTotalCustomers.Dock = DockStyle.Fill;
        _lblTotalCustomers.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblTotalCustomers.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _lblTotalCustomers.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

        _lblActiveCustomers.Dock = DockStyle.Fill;
        _lblActiveCustomers.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblActiveCustomers.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _lblActiveCustomers.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

        _lblWithBalance.Dock = DockStyle.Fill;
        _lblWithBalance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblWithBalance.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _lblWithBalance.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

        _lblTotalOutstanding.Dock = DockStyle.Fill;
        _lblTotalOutstanding.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblTotalOutstanding.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _lblTotalOutstanding.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

        bottomPanel.Controls.Add(_lblTotalCustomers, 0, 0);
        bottomPanel.Controls.Add(_lblActiveCustomers, 1, 0);
        bottomPanel.Controls.Add(_lblWithBalance, 2, 0);
        bottomPanel.Controls.Add(_lblTotalOutstanding, 3, 0);

        // Wire Button Events
        _newButton.Click += NewButton_Click;
        _refreshButton.Click += RefreshButton_Click;
        _exportButton.Click += ExportButton_Click;
        _btnReceivePayment.Click += BtnReceivePayment_Click;
        _btnLedger.Click += BtnLedger_Click;
        _btnToggleStatus.Click += BtnToggleStatus_Click;

        Load += CustomersView_Load;

        // Assemble Root
        root.Controls.Add(topPanel, 0, 0);
        root.Controls.Add(filterPanel, 0, 1);
        root.Controls.Add(_gridControl, 0, 2);
        root.Controls.Add(bottomPanel, 0, 3);
        Controls.Add(root);

        ((System.ComponentModel.ISupportInitialize)_gridControl).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtSearch.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_comboStatus.Properties).EndInit();

        ResumeLayout(false);
    }
}
