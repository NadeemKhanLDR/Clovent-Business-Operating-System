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
    private SearchLookUpEdit _txtSearch;
    private ComboBoxEdit _comboStatus;
    private SimpleButton _btnClearFilters;

    private SimpleButton _newButton;
    private SimpleButton _refreshButton;
    private SimpleButton _exportButton;
    private SimpleButton _btnReceivePayment;
    private SimpleButton _btnLedger;
    private SimpleButton _btnToggleStatus;
    private SimpleButton _btnSetDefault;

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
        _txtSearch = new SearchLookUpEdit();
        _comboStatus = new ComboBoxEdit();
        _btnClearFilters = new SimpleButton();
        _newButton = new SimpleButton();
        _refreshButton = new SimpleButton();
        _exportButton = new SimpleButton();
        _btnReceivePayment = new SimpleButton();
        _btnLedger = new SimpleButton();
        _btnToggleStatus = new SimpleButton();
        _btnSetDefault = new SimpleButton();
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
        colCode = new DevExpress.XtraGrid.Columns.GridColumn();
        colName = new DevExpress.XtraGrid.Columns.GridColumn();
        colMobileNumber = new DevExpress.XtraGrid.Columns.GridColumn();
        colPhone = new DevExpress.XtraGrid.Columns.GridColumn();
        colMobile2 = new DevExpress.XtraGrid.Columns.GridColumn();
        colShopNo = new DevExpress.XtraGrid.Columns.GridColumn();
        colEmail = new DevExpress.XtraGrid.Columns.GridColumn();
        colOutstandingBalance = new DevExpress.XtraGrid.Columns.GridColumn();
        colCreditLimit = new DevExpress.XtraGrid.Columns.GridColumn();
        colStatusText = new DevExpress.XtraGrid.Columns.GridColumn();
        colIsDefaultText = new DevExpress.XtraGrid.Columns.GridColumn();
        colLastTransactionText = new DevExpress.XtraGrid.Columns.GridColumn();
        ((System.ComponentModel.ISupportInitialize)_gridControl).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtSearch.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_comboStatus.Properties).BeginInit();
        root.SuspendLayout();
        topPanel.SuspendLayout();
        actionsPanel.SuspendLayout();
        filterPanel.SuspendLayout();
        bottomPanel.SuspendLayout();
        SuspendLayout();
        // 
        // _gridControl
        // 
        _gridControl.Dock = DockStyle.Fill;
        _gridControl.Location = new Point(3, 103);
        _gridControl.MainView = _gridView;
        _gridControl.Name = "_gridControl";
        _gridControl.Size = new Size(923, 334);
        _gridControl.TabIndex = 2;
        _gridControl.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { _gridView });
        // 
        // _gridView
        // 
        _gridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F);
        _gridView.Appearance.Row.Options.UseFont = true;
        _gridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { colCode, colName, colMobileNumber, colPhone, colMobile2, colShopNo, colEmail, colOutstandingBalance, colCreditLimit, colStatusText, colIsDefaultText, colLastTransactionText });
        _gridView.GridControl = _gridControl;
        _gridView.Name = "_gridView";
        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.RowHeight = 32;
        _gridView.RowCellClick += GridView_RowCellClick;
        _gridView.FocusedRowChanged += GridView_FocusedRowChanged;
        _gridView.CustomColumnDisplayText += GridView_CustomColumnDisplayText;
        // 
        // _txtSearch
        // 
        _txtSearch.Dock = DockStyle.Fill;
        _txtSearch.Location = new Point(15, 7);
        _txtSearch.Name = "_txtSearch";
        _txtSearch.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtSearch.Properties.Appearance.Options.UseFont = true;
        _txtSearch.Properties.DisplayMember = "Name";
        _txtSearch.Properties.NullText = "Search Customer...";
        _txtSearch.Properties.ValueMember = "CustomerId";
        _txtSearch.Size = new Size(314, 24);
        _txtSearch.TabIndex = 0;
        _txtSearch.EditValueChanged += TxtSearch_EditValueChanged;
        // 
        // _comboStatus
        // 
        _comboStatus.Dock = DockStyle.Fill;
        _comboStatus.EditValue = "All";
        _comboStatus.Location = new Point(335, 7);
        _comboStatus.Name = "_comboStatus";
        _comboStatus.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _comboStatus.Properties.Appearance.Options.UseFont = true;
        _comboStatus.Properties.Items.AddRange(new object[] { "All", "Active", "Inactive" });
        _comboStatus.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _comboStatus.Size = new Size(174, 24);
        _comboStatus.TabIndex = 1;
        _comboStatus.SelectedIndexChanged += ComboStatus_SelectedIndexChanged;
        // 
        // _btnClearFilters
        // 
        _btnClearFilters.Appearance.Font = new Font("Segoe UI", 9F);
        _btnClearFilters.Appearance.Options.UseFont = true;
        _btnClearFilters.Dock = DockStyle.Fill;
        _btnClearFilters.Location = new Point(515, 7);
        _btnClearFilters.Name = "_btnClearFilters";
        _btnClearFilters.Size = new Size(104, 25);
        _btnClearFilters.TabIndex = 2;
        _btnClearFilters.Text = "Clear Filters";
        _btnClearFilters.Click += BtnClearFilters_Click;
        // 
        // _newButton
        // 
        _newButton.Appearance.Font = new Font("Segoe UI", 9F);
        _newButton.Appearance.Options.UseFont = true;
        _newButton.Location = new Point(580, 43);
        _newButton.MinimumSize = new Size(130, 32);
        _newButton.Name = "_newButton";
        _newButton.Size = new Size(130, 32);
        _newButton.TabIndex = 6;
        _newButton.Text = "+ New Customer";
        _newButton.Click += NewButton_Click;
        // 
        // _refreshButton
        // 
        _refreshButton.Appearance.Font = new Font("Segoe UI", 9F);
        _refreshButton.Appearance.Options.UseFont = true;
        _refreshButton.Location = new Point(50, 5);
        _refreshButton.MinimumSize = new Size(80, 32);
        _refreshButton.Name = "_refreshButton";
        _refreshButton.Size = new Size(80, 32);
        _refreshButton.TabIndex = 5;
        _refreshButton.Text = "Refresh";
        _refreshButton.Click += RefreshButton_Click;
        // 
        // _exportButton
        // 
        _exportButton.Appearance.Font = new Font("Segoe UI", 9F);
        _exportButton.Appearance.Options.UseFont = true;
        _exportButton.Location = new Point(136, 5);
        _exportButton.MinimumSize = new Size(95, 32);
        _exportButton.Name = "_exportButton";
        _exportButton.Size = new Size(95, 32);
        _exportButton.TabIndex = 4;
        _exportButton.Text = "Export CSV";
        _exportButton.Click += ExportButton_Click;
        // 
        // _btnReceivePayment
        // 
        _btnReceivePayment.Appearance.Font = new Font("Segoe UI", 9F);
        _btnReceivePayment.Appearance.Options.UseFont = true;
        _btnReceivePayment.Location = new Point(464, 5);
        _btnReceivePayment.MinimumSize = new Size(130, 32);
        _btnReceivePayment.Name = "_btnReceivePayment";
        _btnReceivePayment.Size = new Size(130, 32);
        _btnReceivePayment.TabIndex = 1;
        _btnReceivePayment.Text = "Receive Payment";
        _btnReceivePayment.Click += BtnReceivePayment_Click;
        // 
        // _btnLedger
        // 
        _btnLedger.Appearance.Font = new Font("Segoe UI", 9F);
        _btnLedger.Appearance.Options.UseFont = true;
        _btnLedger.Location = new Point(600, 5);
        _btnLedger.MinimumSize = new Size(110, 32);
        _btnLedger.Name = "_btnLedger";
        _btnLedger.Size = new Size(110, 32);
        _btnLedger.TabIndex = 0;
        _btnLedger.Text = "View Ledger";
        _btnLedger.Click += BtnLedger_Click;
        // 
        // _btnToggleStatus
        // 
        _btnToggleStatus.Appearance.Font = new Font("Segoe UI", 9F);
        _btnToggleStatus.Appearance.Options.UseFont = true;
        _btnToggleStatus.Location = new Point(358, 5);
        _btnToggleStatus.MinimumSize = new Size(100, 32);
        _btnToggleStatus.Name = "_btnToggleStatus";
        _btnToggleStatus.Size = new Size(100, 32);
        _btnToggleStatus.TabIndex = 2;
        _btnToggleStatus.Text = "Deactivate";
        _btnToggleStatus.Click += BtnToggleStatus_Click;
        // 
        // _btnSetDefault
        // 
        _btnSetDefault.Appearance.Font = new Font("Segoe UI", 9F);
        _btnSetDefault.Appearance.Options.UseFont = true;
        _btnSetDefault.Location = new Point(237, 5);
        _btnSetDefault.MinimumSize = new Size(115, 32);
        _btnSetDefault.Name = "_btnSetDefault";
        _btnSetDefault.Size = new Size(115, 32);
        _btnSetDefault.TabIndex = 3;
        _btnSetDefault.Text = "Set as Default";
        _btnSetDefault.Click += BtnSetDefault_Click;
        // 
        // _lblTotalCustomers
        // 
        _lblTotalCustomers.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblTotalCustomers.Appearance.Options.UseFont = true;
        _lblTotalCustomers.Appearance.Options.UseTextOptions = true;
        _lblTotalCustomers.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _lblTotalCustomers.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblTotalCustomers.Dock = DockStyle.Fill;
        _lblTotalCustomers.Location = new Point(15, 9);
        _lblTotalCustomers.Name = "_lblTotalCustomers";
        _lblTotalCustomers.Size = new Size(218, 16);
        _lblTotalCustomers.TabIndex = 0;
        // 
        // _lblActiveCustomers
        // 
        _lblActiveCustomers.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblActiveCustomers.Appearance.Options.UseFont = true;
        _lblActiveCustomers.Appearance.Options.UseTextOptions = true;
        _lblActiveCustomers.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _lblActiveCustomers.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblActiveCustomers.Dock = DockStyle.Fill;
        _lblActiveCustomers.Location = new Point(239, 9);
        _lblActiveCustomers.Name = "_lblActiveCustomers";
        _lblActiveCustomers.Size = new Size(218, 16);
        _lblActiveCustomers.TabIndex = 1;
        // 
        // _lblWithBalance
        // 
        _lblWithBalance.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblWithBalance.Appearance.Options.UseFont = true;
        _lblWithBalance.Appearance.Options.UseTextOptions = true;
        _lblWithBalance.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _lblWithBalance.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblWithBalance.Dock = DockStyle.Fill;
        _lblWithBalance.Location = new Point(463, 9);
        _lblWithBalance.Name = "_lblWithBalance";
        _lblWithBalance.Size = new Size(218, 16);
        _lblWithBalance.TabIndex = 2;
        // 
        // _lblTotalOutstanding
        // 
        _lblTotalOutstanding.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblTotalOutstanding.Appearance.Options.UseFont = true;
        _lblTotalOutstanding.Appearance.Options.UseTextOptions = true;
        _lblTotalOutstanding.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _lblTotalOutstanding.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblTotalOutstanding.Dock = DockStyle.Fill;
        _lblTotalOutstanding.Location = new Point(687, 9);
        _lblTotalOutstanding.Name = "_lblTotalOutstanding";
        _lblTotalOutstanding.Size = new Size(221, 16);
        _lblTotalOutstanding.TabIndex = 3;
        // 
        // root
        // 
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        root.Controls.Add(topPanel, 0, 0);
        root.Controls.Add(filterPanel, 0, 1);
        root.Controls.Add(_gridControl, 0, 2);
        root.Controls.Add(bottomPanel, 0, 3);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Name = "root";
        root.RowCount = 4;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        root.Size = new Size(929, 480);
        root.TabIndex = 0;
        // 
        // topPanel
        // 
        topPanel.ColumnCount = 2;
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        topPanel.Controls.Add(headerLabel, 0, 0);
        topPanel.Controls.Add(actionsPanel, 1, 0);
        topPanel.Dock = DockStyle.Fill;
        topPanel.Location = new Point(3, 3);
        topPanel.Name = "topPanel";
        topPanel.Padding = new Padding(12, 6, 12, 6);
        topPanel.RowCount = 1;
        topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        topPanel.Size = new Size(923, 49);
        topPanel.TabIndex = 0;
        // 
        // headerLabel
        // 
        headerLabel.Appearance.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        headerLabel.Appearance.Options.UseFont = true;
        headerLabel.Appearance.Options.UseTextOptions = true;
        headerLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        headerLabel.Dock = DockStyle.Fill;
        headerLabel.Location = new Point(15, 9);
        headerLabel.Name = "headerLabel";
        headerLabel.Size = new Size(174, 31);
        headerLabel.TabIndex = 0;
        headerLabel.Text = "Customers";
        // 
        // actionsPanel
        // 
        actionsPanel.Controls.Add(_btnLedger);
        actionsPanel.Controls.Add(_btnReceivePayment);
        actionsPanel.Controls.Add(_btnToggleStatus);
        actionsPanel.Controls.Add(_btnSetDefault);
        actionsPanel.Controls.Add(_exportButton);
        actionsPanel.Controls.Add(_refreshButton);
        actionsPanel.Controls.Add(_newButton);
        actionsPanel.Dock = DockStyle.Fill;
        actionsPanel.FlowDirection = FlowDirection.RightToLeft;
        actionsPanel.Location = new Point(195, 9);
        actionsPanel.Name = "actionsPanel";
        actionsPanel.Padding = new Padding(0, 2, 0, 0);
        actionsPanel.Size = new Size(713, 31);
        actionsPanel.TabIndex = 1;
        // 
        // filterPanel
        // 
        filterPanel.ColumnCount = 4;
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        filterPanel.Controls.Add(_txtSearch, 0, 0);
        filterPanel.Controls.Add(_comboStatus, 1, 0);
        filterPanel.Controls.Add(_btnClearFilters, 2, 0);
        filterPanel.Dock = DockStyle.Fill;
        filterPanel.Location = new Point(3, 58);
        filterPanel.Name = "filterPanel";
        filterPanel.Padding = new Padding(12, 4, 12, 4);
        filterPanel.RowCount = 1;
        filterPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        filterPanel.Size = new Size(923, 39);
        filterPanel.TabIndex = 1;
        // 
        // bottomPanel
        // 
        bottomPanel.ColumnCount = 4;
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bottomPanel.Controls.Add(_lblTotalCustomers, 0, 0);
        bottomPanel.Controls.Add(_lblActiveCustomers, 1, 0);
        bottomPanel.Controls.Add(_lblWithBalance, 2, 0);
        bottomPanel.Controls.Add(_lblTotalOutstanding, 3, 0);
        bottomPanel.Dock = DockStyle.Fill;
        bottomPanel.Location = new Point(3, 443);
        bottomPanel.Name = "bottomPanel";
        bottomPanel.Padding = new Padding(12, 6, 12, 6);
        bottomPanel.RowCount = 1;
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        bottomPanel.Size = new Size(923, 34);
        bottomPanel.TabIndex = 3;
        // 
        // colCode
        // 
        colCode.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colCode.AppearanceHeader.Options.UseFont = true;
        colCode.Caption = "Code";
        colCode.FieldName = "Code";
        colCode.Name = "colCode";
        colCode.Visible = true;
        colCode.VisibleIndex = 0;
        colCode.Width = 100;
        // 
        // colName
        // 
        colName.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colName.AppearanceHeader.Options.UseFont = true;
        colName.Caption = "Customer Name";
        colName.FieldName = "Name";
        colName.Name = "colName";
        colName.Visible = true;
        colName.VisibleIndex = 1;
        colName.Width = 180;
        // 
        // colMobileNumber
        // 
        colMobileNumber.Caption = "Mobile";
        colMobileNumber.FieldName = "MobileNumber";
        colMobileNumber.Name = "colMobileNumber";
        colMobileNumber.Visible = true;
        colMobileNumber.VisibleIndex = 2;
        // 
        // colPhone
        // 
        colPhone.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colPhone.AppearanceHeader.Options.UseFont = true;
        colPhone.Caption = "Phone";
        colPhone.FieldName = "Phone";
        colPhone.Name = "colPhone";
        colPhone.Visible = true;
        colPhone.VisibleIndex = 3;
        colPhone.Width = 120;
        // 
        // colMobile2
        // 
        colMobile2.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colMobile2.AppearanceHeader.Options.UseFont = true;
        colMobile2.Caption = "Alt. Mobile";
        colMobile2.FieldName = "Mobile2";
        colMobile2.Name = "colMobile2";
        colMobile2.Width = 120;
        // 
        // colShopNo
        // 
        colShopNo.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colShopNo.AppearanceHeader.Options.UseFont = true;
        colShopNo.Caption = "Shop No";
        colShopNo.FieldName = "ShopNo";
        colShopNo.Name = "colShopNo";
        colShopNo.Width = 100;
        // 
        // colEmail
        // 
        colEmail.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        colEmail.AppearanceHeader.Options.UseFont = true;
        colEmail.Caption = "Email";
        colEmail.FieldName = "Email";
        colEmail.Name = "colEmail";
        colEmail.Visible = true;
        colEmail.VisibleIndex = 4;
        colEmail.Width = 160;
        // 
        // colOutstandingBalance
        // 
        colOutstandingBalance.Caption = "Outstanding";
        colOutstandingBalance.FieldName = "OutstandingBalance";
        colOutstandingBalance.Name = "colOutstandingBalance";
        colOutstandingBalance.Visible = true;
        colOutstandingBalance.VisibleIndex = 5;
        // 
        // colCreditLimit
        // 
        colCreditLimit.Caption = "Credit Limit";
        colCreditLimit.FieldName = "CreditLimit";
        colCreditLimit.Name = "colCreditLimit";
        colCreditLimit.Visible = true;
        colCreditLimit.VisibleIndex = 6;
        // 
        // colStatusText
        // 
        colStatusText.Caption = "Status";
        colStatusText.FieldName = "StatusText";
        colStatusText.Name = "colStatusText";
        colStatusText.Visible = true;
        colStatusText.VisibleIndex = 7;
        // 
        // colIsDefaultText
        // 
        colIsDefaultText.Caption = "Default";
        colIsDefaultText.FieldName = "IsDefaultText";
        colIsDefaultText.Name = "colIsDefaultText";
        colIsDefaultText.Visible = true;
        colIsDefaultText.VisibleIndex = 8;
        // 
        // colLastTransactionText
        // 
        colLastTransactionText.Caption = "Last Transaction";
        colLastTransactionText.FieldName = "LastTransactionText";
        colLastTransactionText.Name = "colLastTransactionText";
        colLastTransactionText.Visible = true;
        colLastTransactionText.VisibleIndex = 9;
        // 
        // CustomersView
        // 
        Controls.Add(root);
        Name = "CustomersView";
        Size = new Size(929, 480);
        Load += CustomersView_Load;
        ((System.ComponentModel.ISupportInitialize)_gridControl).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtSearch.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_comboStatus.Properties).EndInit();
        root.ResumeLayout(false);
        topPanel.ResumeLayout(false);
        topPanel.PerformLayout();
        actionsPanel.ResumeLayout(false);
        filterPanel.ResumeLayout(false);
        bottomPanel.ResumeLayout(false);
        bottomPanel.PerformLayout();
        ResumeLayout(false);
    }

    private DevExpress.XtraGrid.Columns.GridColumn colCode;
    private DevExpress.XtraGrid.Columns.GridColumn colName;
    private DevExpress.XtraGrid.Columns.GridColumn colMobileNumber;
    private DevExpress.XtraGrid.Columns.GridColumn colPhone;
    private DevExpress.XtraGrid.Columns.GridColumn colMobile2;
    private DevExpress.XtraGrid.Columns.GridColumn colShopNo;
    private DevExpress.XtraGrid.Columns.GridColumn colEmail;
    private DevExpress.XtraGrid.Columns.GridColumn colOutstandingBalance;
    private DevExpress.XtraGrid.Columns.GridColumn colCreditLimit;
    private DevExpress.XtraGrid.Columns.GridColumn colStatusText;
    private DevExpress.XtraGrid.Columns.GridColumn colIsDefaultText;
    private DevExpress.XtraGrid.Columns.GridColumn colLastTransactionText;
}
