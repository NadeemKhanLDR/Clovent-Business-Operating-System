namespace Clovent.Desktop.Restaurant.Shared;

partial class ManagerAuthorizationForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        _rootLayout = new System.Windows.Forms.TableLayoutPanel();
        _headerPanel = new System.Windows.Forms.Panel();
        _headerLabel = new DevExpress.XtraEditors.LabelControl();
        _lblSubtitle = new DevExpress.XtraEditors.LabelControl();
        _cardPanel = new DevExpress.XtraEditors.PanelControl();
        _summaryTable = new System.Windows.Forms.TableLayoutPanel();
        _lblInstruction = new DevExpress.XtraEditors.LabelControl();
        _detailLabel = new DevExpress.XtraEditors.LabelControl();
        _financialsGrid = new System.Windows.Forms.TableLayoutPanel();
        _lblCustomerTitle = new DevExpress.XtraEditors.LabelControl();
        _lblCustomerVal = new DevExpress.XtraEditors.LabelControl();
        _lblOutstandingTitle = new DevExpress.XtraEditors.LabelControl();
        _lblOutstandingVal = new DevExpress.XtraEditors.LabelControl();
        _lblLimitTitle = new DevExpress.XtraEditors.LabelControl();
        _lblLimitVal = new DevExpress.XtraEditors.LabelControl();
        _lblSaleTitle = new DevExpress.XtraEditors.LabelControl();
        _lblSaleVal = new DevExpress.XtraEditors.LabelControl();
        _lblNewBalanceTitle = new DevExpress.XtraEditors.LabelControl();
        _lblNewBalanceVal = new DevExpress.XtraEditors.LabelControl();
        _lblNotice = new DevExpress.XtraEditors.LabelControl();
        _lblErrorMessage = new DevExpress.XtraEditors.LabelControl();
        _credentialsPanel = new System.Windows.Forms.TableLayoutPanel();
        _lblUserName = new DevExpress.XtraEditors.LabelControl();
        _userNameEdit = new DevExpress.XtraEditors.TextEdit();
        _lblPassword = new DevExpress.XtraEditors.LabelControl();
        _passwordEdit = new DevExpress.XtraEditors.TextEdit();
        _buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
        _btnAuthorize = new DevExpress.XtraEditors.SimpleButton();
        _btnCancel = new DevExpress.XtraEditors.SimpleButton();
        ((System.ComponentModel.ISupportInitialize)_cardPanel).BeginInit();
        _cardPanel.SuspendLayout();
        _summaryTable.SuspendLayout();
        _financialsGrid.SuspendLayout();
        _credentialsPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_userNameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_passwordEdit.Properties).BeginInit();
        _buttonPanel.SuspendLayout();
        _headerPanel.SuspendLayout();
        _rootLayout.SuspendLayout();
        SuspendLayout();
        // 
        // _rootLayout
        // 
        _rootLayout.ColumnCount = 1;
        _rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _rootLayout.Controls.Add(_headerPanel, 0, 0);
        _rootLayout.Controls.Add(_cardPanel, 0, 1);
        _rootLayout.Controls.Add(_lblErrorMessage, 0, 2);
        _rootLayout.Controls.Add(_credentialsPanel, 0, 3);
        _rootLayout.Controls.Add(_buttonPanel, 0, 4);
        _rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        _rootLayout.Location = new System.Drawing.Point(0, 0);
        _rootLayout.Name = "_rootLayout";
        _rootLayout.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
        _rootLayout.RowCount = 5;
        _rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _rootLayout.Size = new System.Drawing.Size(540, 450);
        _rootLayout.TabIndex = 0;
        // 
        // _headerPanel
        // 
        _headerPanel.AutoSize = true;
        _headerPanel.Controls.Add(_lblSubtitle);
        _headerPanel.Controls.Add(_headerLabel);
        _headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
        _headerPanel.Location = new System.Drawing.Point(16, 12);
        _headerPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        _headerPanel.Name = "_headerPanel";
        _headerPanel.Size = new System.Drawing.Size(508, 42);
        _headerPanel.TabIndex = 0;
        // 
        // _headerLabel
        // 
        _headerLabel.Appearance.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
        _headerLabel.Appearance.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
        _headerLabel.Appearance.Options.UseFont = true;
        _headerLabel.Appearance.Options.UseForeColor = true;
        _headerLabel.Dock = System.Windows.Forms.DockStyle.Top;
        _headerLabel.Location = new System.Drawing.Point(0, 0);
        _headerLabel.Name = "_headerLabel";
        _headerLabel.Size = new System.Drawing.Size(161, 20);
        _headerLabel.TabIndex = 0;
        _headerLabel.Text = "Manager Authorization";
        // 
        // _lblSubtitle
        // 
        _lblSubtitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        _lblSubtitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
        _lblSubtitle.Appearance.Options.UseFont = true;
        _lblSubtitle.Appearance.Options.UseForeColor = true;
        _lblSubtitle.Dock = System.Windows.Forms.DockStyle.Top;
        _lblSubtitle.Location = new System.Drawing.Point(0, 20);
        _lblSubtitle.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        _lblSubtitle.Name = "_lblSubtitle";
        _lblSubtitle.Size = new System.Drawing.Size(115, 13);
        _lblSubtitle.TabIndex = 1;
        _lblSubtitle.Text = "Credit Limit Override";
        // 
        // _cardPanel
        // 
        _cardPanel.AutoSize = true;
        _cardPanel.Controls.Add(_summaryTable);
        _cardPanel.Dock = System.Windows.Forms.DockStyle.Top;
        _cardPanel.Location = new System.Drawing.Point(16, 64);
        _cardPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        _cardPanel.Name = "_cardPanel";
        _cardPanel.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
        _cardPanel.Size = new System.Drawing.Size(508, 185);
        _cardPanel.TabIndex = 1;
        // 
        // _summaryTable
        // 
        _summaryTable.AutoSize = true;
        _summaryTable.ColumnCount = 1;
        _summaryTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _summaryTable.Controls.Add(_lblInstruction, 0, 0);
        _summaryTable.Controls.Add(_detailLabel, 0, 1);
        _summaryTable.Controls.Add(_financialsGrid, 0, 2);
        _summaryTable.Controls.Add(_lblNotice, 0, 3);
        _summaryTable.Dock = System.Windows.Forms.DockStyle.Fill;
        _summaryTable.Location = new System.Drawing.Point(14, 12);
        _summaryTable.Margin = new System.Windows.Forms.Padding(0);
        _summaryTable.Name = "_summaryTable";
        _summaryTable.RowCount = 4;
        _summaryTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _summaryTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _summaryTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _summaryTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _summaryTable.Size = new System.Drawing.Size(480, 161);
        _summaryTable.TabIndex = 0;
        // 
        // _lblInstruction
        // 
        _lblInstruction.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
        _lblInstruction.Appearance.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
        _lblInstruction.Appearance.Options.UseFont = true;
        _lblInstruction.Appearance.Options.UseForeColor = true;
        _lblInstruction.Appearance.Options.UseTextOptions = true;
        _lblInstruction.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        _lblInstruction.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
        _lblInstruction.Dock = System.Windows.Forms.DockStyle.Top;
        _lblInstruction.Location = new System.Drawing.Point(0, 0);
        _lblInstruction.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
        _lblInstruction.Name = "_lblInstruction";
        _lblInstruction.Size = new System.Drawing.Size(480, 13);
        _lblInstruction.TabIndex = 0;
        _lblInstruction.Text = "Manager authorization is required to approve this credit sale.";
        // 
        // _detailLabel
        // 
        _detailLabel.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        _detailLabel.Appearance.Options.UseFont = true;
        _detailLabel.Appearance.Options.UseTextOptions = true;
        _detailLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        _detailLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
        _detailLabel.Dock = System.Windows.Forms.DockStyle.Top;
        _detailLabel.Location = new System.Drawing.Point(0, 19);
        _detailLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
        _detailLabel.Name = "_detailLabel";
        _detailLabel.Size = new System.Drawing.Size(480, 15);
        _detailLabel.TabIndex = 1;
        _detailLabel.Visible = false;
        // 
        // _financialsGrid
        // 
        _financialsGrid.AutoSize = true;
        _financialsGrid.ColumnCount = 2;
        _financialsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
        _financialsGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _financialsGrid.Controls.Add(_lblCustomerTitle, 0, 0);
        _financialsGrid.Controls.Add(_lblCustomerVal, 1, 0);
        _financialsGrid.Controls.Add(_lblOutstandingTitle, 0, 1);
        _financialsGrid.Controls.Add(_lblOutstandingVal, 1, 1);
        _financialsGrid.Controls.Add(_lblLimitTitle, 0, 2);
        _financialsGrid.Controls.Add(_lblLimitVal, 1, 2);
        _financialsGrid.Controls.Add(_lblSaleTitle, 0, 3);
        _financialsGrid.Controls.Add(_lblSaleVal, 1, 3);
        _financialsGrid.Controls.Add(_lblNewBalanceTitle, 0, 4);
        _financialsGrid.Controls.Add(_lblNewBalanceVal, 1, 4);
        _financialsGrid.Dock = System.Windows.Forms.DockStyle.Top;
        _financialsGrid.Location = new System.Drawing.Point(0, 40);
        _financialsGrid.Margin = new System.Windows.Forms.Padding(0, 2, 0, 4);
        _financialsGrid.Name = "_financialsGrid";
        _financialsGrid.RowCount = 5;
        _financialsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _financialsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _financialsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _financialsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _financialsGrid.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _financialsGrid.Size = new System.Drawing.Size(480, 110);
        _financialsGrid.TabIndex = 2;
        // 
        // _lblCustomerTitle
        // 
        _lblCustomerTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
        _lblCustomerTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
        _lblCustomerTitle.Appearance.Options.UseFont = true;
        _lblCustomerTitle.Appearance.Options.UseForeColor = true;
        _lblCustomerTitle.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblCustomerTitle.Location = new System.Drawing.Point(3, 3);
        _lblCustomerTitle.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
        _lblCustomerTitle.Name = "_lblCustomerTitle";
        _lblCustomerTitle.Size = new System.Drawing.Size(55, 15);
        _lblCustomerTitle.TabIndex = 0;
        _lblCustomerTitle.Text = "Customer:";
        // 
        // _lblCustomerVal
        // 
        _lblCustomerVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
        _lblCustomerVal.Appearance.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
        _lblCustomerVal.Appearance.Options.UseFont = true;
        _lblCustomerVal.Appearance.Options.UseForeColor = true;
        _lblCustomerVal.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblCustomerVal.Location = new System.Drawing.Point(163, 3);
        _lblCustomerVal.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
        _lblCustomerVal.Name = "_lblCustomerVal";
        _lblCustomerVal.Size = new System.Drawing.Size(6, 15);
        _lblCustomerVal.TabIndex = 1;
        _lblCustomerVal.Text = "-";
        // 
        // _lblOutstandingTitle
        // 
        _lblOutstandingTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
        _lblOutstandingTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
        _lblOutstandingTitle.Appearance.Options.UseFont = true;
        _lblOutstandingTitle.Appearance.Options.UseForeColor = true;
        _lblOutstandingTitle.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblOutstandingTitle.Location = new System.Drawing.Point(3, 24);
        _lblOutstandingTitle.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
        _lblOutstandingTitle.Name = "_lblOutstandingTitle";
        _lblOutstandingTitle.Size = new System.Drawing.Size(109, 15);
        _lblOutstandingTitle.TabIndex = 2;
        _lblOutstandingTitle.Text = "Current Outstanding:";
        // 
        // _lblOutstandingVal
        // 
        _lblOutstandingVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
        _lblOutstandingVal.Appearance.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
        _lblOutstandingVal.Appearance.Options.UseFont = true;
        _lblOutstandingVal.Appearance.Options.UseForeColor = true;
        _lblOutstandingVal.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblOutstandingVal.Location = new System.Drawing.Point(163, 24);
        _lblOutstandingVal.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
        _lblOutstandingVal.Name = "_lblOutstandingVal";
        _lblOutstandingVal.Size = new System.Drawing.Size(6, 15);
        _lblOutstandingVal.TabIndex = 3;
        _lblOutstandingVal.Text = "-";
        // 
        // _lblLimitTitle
        // 
        _lblLimitTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
        _lblLimitTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
        _lblLimitTitle.Appearance.Options.UseFont = true;
        _lblLimitTitle.Appearance.Options.UseForeColor = true;
        _lblLimitTitle.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblLimitTitle.Location = new System.Drawing.Point(3, 45);
        _lblLimitTitle.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
        _lblLimitTitle.Name = "_lblLimitTitle";
        _lblLimitTitle.Size = new System.Drawing.Size(105, 15);
        _lblLimitTitle.TabIndex = 4;
        _lblLimitTitle.Text = "Current Credit Limit:";
        // 
        // _lblLimitVal
        // 
        _lblLimitVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
        _lblLimitVal.Appearance.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
        _lblLimitVal.Appearance.Options.UseFont = true;
        _lblLimitVal.Appearance.Options.UseForeColor = true;
        _lblLimitVal.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblLimitVal.Location = new System.Drawing.Point(163, 45);
        _lblLimitVal.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
        _lblLimitVal.Name = "_lblLimitVal";
        _lblLimitVal.Size = new System.Drawing.Size(6, 15);
        _lblLimitVal.TabIndex = 5;
        _lblLimitVal.Text = "-";
        // 
        // _lblSaleTitle
        // 
        _lblSaleTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
        _lblSaleTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
        _lblSaleTitle.Appearance.Options.UseFont = true;
        _lblSaleTitle.Appearance.Options.UseForeColor = true;
        _lblSaleTitle.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblSaleTitle.Location = new System.Drawing.Point(3, 66);
        _lblSaleTitle.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
        _lblSaleTitle.Name = "_lblSaleTitle";
        _lblSaleTitle.Size = new System.Drawing.Size(95, 15);
        _lblSaleTitle.TabIndex = 6;
        _lblSaleTitle.Text = "New Sale Amount:";
        // 
        // _lblSaleVal
        // 
        _lblSaleVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
        _lblSaleVal.Appearance.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
        _lblSaleVal.Appearance.Options.UseFont = true;
        _lblSaleVal.Appearance.Options.UseForeColor = true;
        _lblSaleVal.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblSaleVal.Location = new System.Drawing.Point(163, 66);
        _lblSaleVal.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
        _lblSaleVal.Name = "_lblSaleVal";
        _lblSaleVal.Size = new System.Drawing.Size(6, 15);
        _lblSaleVal.TabIndex = 7;
        _lblSaleVal.Text = "-";
        // 
        // _lblNewBalanceTitle
        // 
        _lblNewBalanceTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
        _lblNewBalanceTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
        _lblNewBalanceTitle.Appearance.Options.UseFont = true;
        _lblNewBalanceTitle.Appearance.Options.UseForeColor = true;
        _lblNewBalanceTitle.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblNewBalanceTitle.Location = new System.Drawing.Point(3, 87);
        _lblNewBalanceTitle.Margin = new System.Windows.Forms.Padding(2, 4, 2, 4);
        _lblNewBalanceTitle.Name = "_lblNewBalanceTitle";
        _lblNewBalanceTitle.Size = new System.Drawing.Size(97, 15);
        _lblNewBalanceTitle.TabIndex = 8;
        _lblNewBalanceTitle.Text = "Balance After Sale:";
        // 
        // _lblNewBalanceVal
        // 
        _lblNewBalanceVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
        _lblNewBalanceVal.Appearance.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
        _lblNewBalanceVal.Appearance.Options.UseFont = true;
        _lblNewBalanceVal.Appearance.Options.UseForeColor = true;
        _lblNewBalanceVal.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblNewBalanceVal.Location = new System.Drawing.Point(163, 87);
        _lblNewBalanceVal.Margin = new System.Windows.Forms.Padding(2, 4, 2, 4);
        _lblNewBalanceVal.Name = "_lblNewBalanceVal";
        _lblNewBalanceVal.Size = new System.Drawing.Size(6, 15);
        _lblNewBalanceVal.TabIndex = 9;
        _lblNewBalanceVal.Text = "-";
        // 
        // _lblNotice
        // 
        _lblNotice.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
        _lblNotice.Appearance.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
        _lblNotice.Appearance.Options.UseFont = true;
        _lblNotice.Appearance.Options.UseForeColor = true;
        _lblNotice.Dock = System.Windows.Forms.DockStyle.Top;
        _lblNotice.Location = new System.Drawing.Point(0, 154);
        _lblNotice.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
        _lblNotice.Name = "_lblNotice";
        _lblNotice.Size = new System.Drawing.Size(127, 17);
        _lblNotice.TabIndex = 3;
        _lblNotice.Text = "Credit limit exceeded.";
        // 
        // _lblErrorMessage
        // 
        _lblErrorMessage.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
        _lblErrorMessage.Appearance.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
        _lblErrorMessage.Appearance.Options.UseFont = true;
        _lblErrorMessage.Appearance.Options.UseForeColor = true;
        _lblErrorMessage.Appearance.Options.UseTextOptions = true;
        _lblErrorMessage.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        _lblErrorMessage.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
        _lblErrorMessage.Dock = System.Windows.Forms.DockStyle.Top;
        _lblErrorMessage.Location = new System.Drawing.Point(16, 259);
        _lblErrorMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
        _lblErrorMessage.Name = "_lblErrorMessage";
        _lblErrorMessage.Size = new System.Drawing.Size(508, 0);
        _lblErrorMessage.TabIndex = 2;
        _lblErrorMessage.Visible = false;
        // 
        // _credentialsPanel
        // 
        _credentialsPanel.AutoSize = true;
        _credentialsPanel.ColumnCount = 2;
        _credentialsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
        _credentialsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _credentialsPanel.Controls.Add(_lblUserName, 0, 0);
        _credentialsPanel.Controls.Add(_userNameEdit, 1, 0);
        _credentialsPanel.Controls.Add(_lblPassword, 0, 1);
        _credentialsPanel.Controls.Add(_passwordEdit, 1, 1);
        _credentialsPanel.Dock = System.Windows.Forms.DockStyle.Top;
        _credentialsPanel.Location = new System.Drawing.Point(16, 267);
        _credentialsPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        _credentialsPanel.Name = "_credentialsPanel";
        _credentialsPanel.RowCount = 2;
        _credentialsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _credentialsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _credentialsPanel.Size = new System.Drawing.Size(508, 72);
        _credentialsPanel.TabIndex = 3;
        // 
        // _lblUserName
        // 
        _lblUserName.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
        _lblUserName.Appearance.Options.UseFont = true;
        _lblUserName.Appearance.Options.UseTextOptions = true;
        _lblUserName.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblUserName.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
        _lblUserName.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblUserName.Location = new System.Drawing.Point(3, 0);
        _lblUserName.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
        _lblUserName.Name = "_lblUserName";
        _lblUserName.Size = new System.Drawing.Size(144, 34);
        _lblUserName.TabIndex = 0;
        _lblUserName.Text = "Manager Username:";
        // 
        // _userNameEdit
        // 
        _userNameEdit.Dock = System.Windows.Forms.DockStyle.Fill;
        _userNameEdit.Location = new System.Drawing.Point(153, 3);
        _userNameEdit.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
        _userNameEdit.Name = "_userNameEdit";
        _userNameEdit.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        _userNameEdit.Properties.Appearance.Options.UseFont = true;
        _userNameEdit.Size = new System.Drawing.Size(352, 28);
        _userNameEdit.TabIndex = 0;
        // 
        // _lblPassword
        // 
        _lblPassword.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
        _lblPassword.Appearance.Options.UseFont = true;
        _lblPassword.Appearance.Options.UseTextOptions = true;
        _lblPassword.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblPassword.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
        _lblPassword.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblPassword.Location = new System.Drawing.Point(3, 37);
        _lblPassword.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
        _lblPassword.Name = "_lblPassword";
        _lblPassword.Size = new System.Drawing.Size(144, 34);
        _lblPassword.TabIndex = 2;
        _lblPassword.Text = "Manager Password:";
        // 
        // _passwordEdit
        // 
        _passwordEdit.Dock = System.Windows.Forms.DockStyle.Fill;
        _passwordEdit.Location = new System.Drawing.Point(153, 40);
        _passwordEdit.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
        _passwordEdit.Name = "_passwordEdit";
        _passwordEdit.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        _passwordEdit.Properties.Appearance.Options.UseFont = true;
        _passwordEdit.Properties.UseSystemPasswordChar = true;
        _passwordEdit.Size = new System.Drawing.Size(352, 28);
        _passwordEdit.TabIndex = 1;
        // 
        // _buttonPanel
        // 
        _buttonPanel.AutoSize = true;
        _buttonPanel.Controls.Add(_btnAuthorize);
        _buttonPanel.Controls.Add(_btnCancel);
        _buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        _buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        _buttonPanel.Location = new System.Drawing.Point(16, 350);
        _buttonPanel.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
        _buttonPanel.Name = "_buttonPanel";
        _buttonPanel.Size = new System.Drawing.Size(508, 40);
        _buttonPanel.TabIndex = 4;
        // 
        // _btnAuthorize
        // 
        _btnAuthorize.Appearance.BackColor = System.Drawing.Color.FromArgb(13, 148, 136);
        _btnAuthorize.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
        _btnAuthorize.Appearance.ForeColor = System.Drawing.Color.White;
        _btnAuthorize.Appearance.Options.UseBackColor = true;
        _btnAuthorize.Appearance.Options.UseFont = true;
        _btnAuthorize.Appearance.Options.UseForeColor = true;
        _btnAuthorize.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
        _btnAuthorize.LookAndFeel.UseDefaultLookAndFeel = false;
        _btnAuthorize.Location = new System.Drawing.Point(398, 3);
        _btnAuthorize.Name = "_btnAuthorize";
        _btnAuthorize.Size = new System.Drawing.Size(107, 34);
        _btnAuthorize.TabIndex = 2;
        _btnAuthorize.Text = "Authorize";
        // 
        // _btnCancel
        // 
        _btnCancel.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
        _btnCancel.Appearance.Options.UseFont = true;
        _btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        _btnCancel.Location = new System.Drawing.Point(305, 3);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new System.Drawing.Size(87, 34);
        _btnCancel.TabIndex = 3;
        _btnCancel.Text = "Cancel";
        // 
        // ManagerAuthorizationForm
        // 
        AcceptButton = _btnAuthorize;
        CancelButton = _btnCancel;
        ClientSize = new System.Drawing.Size(540, 450);
        Controls.Add(_rootLayout);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new System.Drawing.Size(480, 380);
        Name = "ManagerAuthorizationForm";
        ShowInTaskbar = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Text = "Manager Authorization";
        ((System.ComponentModel.ISupportInitialize)_cardPanel).EndInit();
        _cardPanel.ResumeLayout(false);
        _cardPanel.PerformLayout();
        _summaryTable.ResumeLayout(false);
        _summaryTable.PerformLayout();
        _financialsGrid.ResumeLayout(false);
        _financialsGrid.PerformLayout();
        _credentialsPanel.ResumeLayout(false);
        _credentialsPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_userNameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_passwordEdit.Properties).EndInit();
        _buttonPanel.ResumeLayout(false);
        _headerPanel.ResumeLayout(false);
        _headerPanel.PerformLayout();
        _rootLayout.ResumeLayout(false);
        _rootLayout.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel _rootLayout;
    private System.Windows.Forms.Panel _headerPanel;
    private DevExpress.XtraEditors.LabelControl _headerLabel;
    private DevExpress.XtraEditors.LabelControl _lblSubtitle;
    private DevExpress.XtraEditors.PanelControl _cardPanel;
    private System.Windows.Forms.TableLayoutPanel _summaryTable;
    private DevExpress.XtraEditors.LabelControl _lblNotice;
    private System.Windows.Forms.TableLayoutPanel _financialsGrid;
    private DevExpress.XtraEditors.LabelControl _lblCustomerTitle;
    private DevExpress.XtraEditors.LabelControl _lblCustomerVal;
    private DevExpress.XtraEditors.LabelControl _lblOutstandingTitle;
    private DevExpress.XtraEditors.LabelControl _lblOutstandingVal;
    private DevExpress.XtraEditors.LabelControl _lblLimitTitle;
    private DevExpress.XtraEditors.LabelControl _lblLimitVal;
    private DevExpress.XtraEditors.LabelControl _lblSaleTitle;
    private DevExpress.XtraEditors.LabelControl _lblSaleVal;
    private DevExpress.XtraEditors.LabelControl _lblNewBalanceTitle;
    private DevExpress.XtraEditors.LabelControl _lblNewBalanceVal;
    private DevExpress.XtraEditors.LabelControl _detailLabel;
    private DevExpress.XtraEditors.LabelControl _lblInstruction;
    private DevExpress.XtraEditors.LabelControl _lblErrorMessage;
    private System.Windows.Forms.TableLayoutPanel _credentialsPanel;
    private DevExpress.XtraEditors.LabelControl _lblUserName;
    private DevExpress.XtraEditors.TextEdit _userNameEdit;
    private DevExpress.XtraEditors.LabelControl _lblPassword;
    private DevExpress.XtraEditors.TextEdit _passwordEdit;
    private System.Windows.Forms.FlowLayoutPanel _buttonPanel;
    private DevExpress.XtraEditors.SimpleButton _btnAuthorize;
    private DevExpress.XtraEditors.SimpleButton _btnCancel;
}
