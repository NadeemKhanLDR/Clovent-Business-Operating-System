using Clovent.Desktop.Forms.Base;

namespace Clovent.Desktop.Forms.Dashboard;

partial class DashboardView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    /// <remarks>
    /// Structure (outermost to innermost, all via <see cref="TableLayoutPanel"/>/
    /// <see cref="FlowLayoutPanel"/>/<c>Dock</c> - no hand-placed
    /// <c>Location</c>/<c>Size</c> pixel coordinates anywhere below): one
    /// outer <c>tlpMain</c> (5 rows: title bar, current-context strip, KPI
    /// card grid, activity/notification/stock/top-selling lists, bottom
    /// toolbar). Every row is a different <see cref="TableLayoutPanel"/>
    /// row index, never competing <c>Dock</c> siblings, so there is no
    /// add-order ambiguity anywhere in this file.
    /// <para>
    /// Deliberately does NOT set <c>AutoScaleMode</c>/<c>AutoScaleDimensions</c> -
    /// combining WinForms' classic Font-based AutoScale with this app's
    /// <c>ApplicationHighDpiMode=PerMonitorV2</c> caused the Designer to
    /// recompute and permanently corrupt every literal coordinate in this
    /// file the moment it was opened on a display running at a different
    /// scale than the baked-in baseline (confirmed on a 250% display - KPI
    /// cards collapsed to 1px tall). Relying on
    /// <c>ApplicationHighDpiMode=PerMonitorV2</c> alone (already declared in
    /// the csproj) plus DevExpress's own native per-monitor-DPI control
    /// rendering avoids this entirely.
    /// </para>
    /// </remarks>
    private void InitializeComponent()
    {
        scrollHost = new Panel();
        tlpMain = new TableLayoutPanel();
        pnlTitleBar = new DevExpress.XtraEditors.PanelControl();
        lblDashboardTitle = new DevExpress.XtraEditors.LabelControl();
        prgLoading = new DevExpress.XtraEditors.ProgressBarControl();
        tlpContext = new TableLayoutPanel();
        lblOrganizationCaption = new DevExpress.XtraEditors.LabelControl();
        lblCompanyCaption = new DevExpress.XtraEditors.LabelControl();
        lblBranchCaption = new DevExpress.XtraEditors.LabelControl();
        lblFiscalYearCaption = new DevExpress.XtraEditors.LabelControl();
        lblCurrentUserCaption = new DevExpress.XtraEditors.LabelControl();
        lblOrganizationValue = new DevExpress.XtraEditors.LabelControl();
        lblCompanyValue = new DevExpress.XtraEditors.LabelControl();
        lblBranchValue = new DevExpress.XtraEditors.LabelControl();
        lblFiscalYearValue = new DevExpress.XtraEditors.LabelControl();
        lblCurrentUserValue = new DevExpress.XtraEditors.LabelControl();
        tlpKpi = new TableLayoutPanel();
        pnlActiveSessions = new DevExpress.XtraEditors.PanelControl();
        lblActiveSessionsValue = new DevExpress.XtraEditors.LabelControl();
        lblActiveSessionsCaption = new DevExpress.XtraEditors.LabelControl();
        pnlRecentLogins = new DevExpress.XtraEditors.PanelControl();
        lblRecentLoginsValue = new DevExpress.XtraEditors.LabelControl();
        lblRecentLoginsCaption = new DevExpress.XtraEditors.LabelControl();
        pnlNotificationsCount = new DevExpress.XtraEditors.PanelControl();
        lblNotificationsCountValue = new DevExpress.XtraEditors.LabelControl();
        lblNotificationsCountCaption = new DevExpress.XtraEditors.LabelControl();
        pnlTotalProducts = new DevExpress.XtraEditors.PanelControl();
        lblTotalProductsValue = new DevExpress.XtraEditors.LabelControl();
        lblTotalProductsCaption = new DevExpress.XtraEditors.LabelControl();
        pnlLowStock = new DevExpress.XtraEditors.PanelControl();
        lblLowStockValue = new DevExpress.XtraEditors.LabelControl();
        lblLowStockCaption = new DevExpress.XtraEditors.LabelControl();
        pnlOutOfStock = new DevExpress.XtraEditors.PanelControl();
        lblOutOfStockValue = new DevExpress.XtraEditors.LabelControl();
        lblOutOfStockCaption = new DevExpress.XtraEditors.LabelControl();
        pnlInventoryValue = new DevExpress.XtraEditors.PanelControl();
        lblInventoryValueValue = new DevExpress.XtraEditors.LabelControl();
        lblInventoryValueCaption = new DevExpress.XtraEditors.LabelControl();
        pnlTodaysSales = new DevExpress.XtraEditors.PanelControl();
        lblTodaysSalesValue = new DevExpress.XtraEditors.LabelControl();
        lblTodaysSalesCaption = new DevExpress.XtraEditors.LabelControl();
        pnlOpenTables = new DevExpress.XtraEditors.PanelControl();
        lblOpenTablesValue = new DevExpress.XtraEditors.LabelControl();
        lblOpenTablesCaption = new DevExpress.XtraEditors.LabelControl();
        pnlRunningOrders = new DevExpress.XtraEditors.PanelControl();
        lblRunningOrdersValue = new DevExpress.XtraEditors.LabelControl();
        lblRunningOrdersCaption = new DevExpress.XtraEditors.LabelControl();
        pnlKitchenQueue = new DevExpress.XtraEditors.PanelControl();
        lblKitchenQueueValue = new DevExpress.XtraEditors.LabelControl();
        lblKitchenQueueCaption = new DevExpress.XtraEditors.LabelControl();
        tlpLists = new TableLayoutPanel();
        pnlRecentActivity = new DevExpress.XtraEditors.PanelControl();
        lstRecentActivity = new DevExpress.XtraEditors.ListBoxControl();
        lblRecentActivityCaption = new DevExpress.XtraEditors.LabelControl();
        pnlNotificationsList = new DevExpress.XtraEditors.PanelControl();
        lstNotifications = new DevExpress.XtraEditors.ListBoxControl();
        lblNotificationsListCaption = new DevExpress.XtraEditors.LabelControl();
        pnlStockMovements = new DevExpress.XtraEditors.PanelControl();
        lstStockMovements = new DevExpress.XtraEditors.ListBoxControl();
        lblStockMovementsCaption = new DevExpress.XtraEditors.LabelControl();
        pnlTopSellingItems = new DevExpress.XtraEditors.PanelControl();
        lstTopSellingItems = new DevExpress.XtraEditors.ListBoxControl();
        lblTopSellingItemsCaption = new DevExpress.XtraEditors.LabelControl();
        flowBottom = new FlowLayoutPanel();
        lblCompanySelectorCaption = new DevExpress.XtraEditors.LabelControl();
        cmbCompany = new DevExpress.XtraEditors.ComboBoxEdit();
        lblBranchSelectorCaption = new DevExpress.XtraEditors.LabelControl();
        cmbBranch = new DevExpress.XtraEditors.ComboBoxEdit();
        btnRefresh = new DevExpress.XtraEditors.SimpleButton();
        btnViewNotifications = new DevExpress.XtraEditors.SimpleButton();
        ((System.ComponentModel.ISupportInitialize)ToolbarPanel).BeginInit();
        ((System.ComponentModel.ISupportInitialize)ContentPanel).BeginInit();
        ContentPanel.SuspendLayout();
        scrollHost.SuspendLayout();
        tlpMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlTitleBar).BeginInit();
        pnlTitleBar.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)prgLoading.Properties).BeginInit();
        tlpContext.SuspendLayout();
        tlpKpi.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlActiveSessions).BeginInit();
        pnlActiveSessions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlRecentLogins).BeginInit();
        pnlRecentLogins.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlNotificationsCount).BeginInit();
        pnlNotificationsCount.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlTotalProducts).BeginInit();
        pnlTotalProducts.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlLowStock).BeginInit();
        pnlLowStock.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlOutOfStock).BeginInit();
        pnlOutOfStock.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlInventoryValue).BeginInit();
        pnlInventoryValue.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlTodaysSales).BeginInit();
        pnlTodaysSales.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlOpenTables).BeginInit();
        pnlOpenTables.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlRunningOrders).BeginInit();
        pnlRunningOrders.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlKitchenQueue).BeginInit();
        pnlKitchenQueue.SuspendLayout();
        tlpLists.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pnlRecentActivity).BeginInit();
        pnlRecentActivity.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)lstRecentActivity).BeginInit();
        ((System.ComponentModel.ISupportInitialize)pnlNotificationsList).BeginInit();
        pnlNotificationsList.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)lstNotifications).BeginInit();
        ((System.ComponentModel.ISupportInitialize)pnlStockMovements).BeginInit();
        pnlStockMovements.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)lstStockMovements).BeginInit();
        ((System.ComponentModel.ISupportInitialize)pnlTopSellingItems).BeginInit();
        pnlTopSellingItems.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)lstTopSellingItems).BeginInit();
        flowBottom.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)cmbCompany.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)cmbBranch.Properties).BeginInit();
        SuspendLayout();
        // 
        // ToolbarPanel
        // 
        ToolbarPanel.Size = new Size(2052, 0);
        // 
        // ContentPanel
        // 
        ContentPanel.Controls.Add(scrollHost);
        ContentPanel.Location = new Point(0, 0);
        ContentPanel.Size = new Size(2052, 1499);
        // 
        // scrollHost
        // 
        scrollHost.AutoScroll = true;
        scrollHost.Controls.Add(tlpMain);
        scrollHost.Dock = DockStyle.Fill;
        scrollHost.Location = new Point(3, 3);
        scrollHost.Name = "scrollHost";
        scrollHost.Size = new Size(2046, 1493);
        scrollHost.TabIndex = 0;
        // 
        // tlpMain
        // 
        tlpMain.ColumnCount = 1;
        tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpMain.Controls.Add(pnlTitleBar, 0, 0);
        tlpMain.Controls.Add(tlpContext, 0, 1);
        tlpMain.Controls.Add(tlpKpi, 0, 2);
        tlpMain.Controls.Add(tlpLists, 0, 3);
        tlpMain.Controls.Add(flowBottom, 0, 4);
        tlpMain.Dock = DockStyle.Fill;
        tlpMain.Location = new Point(0, 0);
        tlpMain.MinimumSize = new Size(0, 700);
        tlpMain.Name = "tlpMain";
        tlpMain.Padding = new Padding(8);
        tlpMain.RowCount = 5;
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
        tlpMain.RowStyles.Add(new RowStyle());
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 443F));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpMain.RowStyles.Add(new RowStyle());
        tlpMain.Size = new Size(2046, 1493);
        tlpMain.TabIndex = 0;
        // 
        // pnlTitleBar
        // 
        pnlTitleBar.Controls.Add(lblDashboardTitle);
        pnlTitleBar.Controls.Add(prgLoading);
        pnlTitleBar.Dock = DockStyle.Top;
        pnlTitleBar.Location = new Point(8, 8);
        pnlTitleBar.Margin = new Padding(0, 0, 0, 8);
        pnlTitleBar.Name = "pnlTitleBar";
        pnlTitleBar.Size = new Size(2030, 91);
        pnlTitleBar.TabIndex = 0;
        // 
        // lblDashboardTitle
        // 
        lblDashboardTitle.Appearance.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        lblDashboardTitle.Appearance.Options.UseFont = true;
        lblDashboardTitle.Appearance.Options.UseTextOptions = true;
        lblDashboardTitle.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblDashboardTitle.Dock = DockStyle.Left;
        lblDashboardTitle.Location = new Point(3, 3);
        lblDashboardTitle.Name = "lblDashboardTitle";
        lblDashboardTitle.Padding = new Padding(4, 0, 24, 0);
        lblDashboardTitle.Size = new Size(333, 81);
        lblDashboardTitle.TabIndex = 0;
        lblDashboardTitle.Text = "Dashboard";
        // 
        // prgLoading
        // 
        prgLoading.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        prgLoading.Location = new Point(1854, 46);
        prgLoading.Name = "prgLoading";
        prgLoading.Size = new Size(160, 18);
        prgLoading.TabIndex = 1;
        prgLoading.Visible = false;
        // 
        // tlpContext
        // 
        tlpContext.AutoSize = true;
        tlpContext.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        tlpContext.ColumnCount = 5;
        tlpContext.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        tlpContext.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        tlpContext.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        tlpContext.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        tlpContext.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        tlpContext.Controls.Add(lblOrganizationCaption, 0, 0);
        tlpContext.Controls.Add(lblCompanyCaption, 1, 0);
        tlpContext.Controls.Add(lblBranchCaption, 2, 0);
        tlpContext.Controls.Add(lblFiscalYearCaption, 3, 0);
        tlpContext.Controls.Add(lblCurrentUserCaption, 4, 0);
        tlpContext.Controls.Add(lblOrganizationValue, 0, 1);
        tlpContext.Controls.Add(lblCompanyValue, 1, 1);
        tlpContext.Controls.Add(lblBranchValue, 2, 1);
        tlpContext.Controls.Add(lblFiscalYearValue, 3, 1);
        tlpContext.Controls.Add(lblCurrentUserValue, 4, 1);
        tlpContext.Dock = DockStyle.Fill;
        tlpContext.Location = new Point(8, 140);
        tlpContext.Margin = new Padding(0, 0, 0, 8);
        tlpContext.Name = "tlpContext";
        tlpContext.Padding = new Padding(0, 8, 0, 8);
        tlpContext.RowCount = 2;
        // AutoSize, not Absolute: the captions/values are DevExpress
        // LabelControls whose fonts scale with the monitor under
        // ApplicationHighDpiMode=PerMonitorV2. Fixed Absolute heights
        // (previously 24F/32F) are designer-DPI pixels that never scale,
        // so at >=125% the taller text overflowed its cell and was
        // overpainted by the next row's label (and at higher DPI by the
        // KPI region) - the captions looked "clipped behind the KPI
        // cards". AutoSize makes each row grow to its label's current
        // preferred height at whatever DPI the app is running, and
        // tlpContext.AutoSize (set above) + tlpMain's own AutoSize row 1
        // propagate that height so the whole strip pushes tlpKpi down
        // instead of being overpainted. See DashboardLayoutRegressionTests.
        tlpContext.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpContext.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpContext.Size = new Size(2030, 72);
        tlpContext.TabIndex = 1;
        // 
        // lblOrganizationCaption
        // 
        lblOrganizationCaption.Appearance.ForeColor = Color.Gray;
        lblOrganizationCaption.Appearance.Options.UseForeColor = true;
        lblOrganizationCaption.Dock = DockStyle.Fill;
        lblOrganizationCaption.Location = new Point(3, 11);
        lblOrganizationCaption.Name = "lblOrganizationCaption";
        lblOrganizationCaption.Size = new Size(400, 18);
        lblOrganizationCaption.TabIndex = 0;
        lblOrganizationCaption.Text = "Current Organization";
        // 
        // lblCompanyCaption
        // 
        lblCompanyCaption.Appearance.ForeColor = Color.Gray;
        lblCompanyCaption.Appearance.Options.UseForeColor = true;
        lblCompanyCaption.Dock = DockStyle.Fill;
        lblCompanyCaption.Location = new Point(409, 11);
        lblCompanyCaption.Name = "lblCompanyCaption";
        lblCompanyCaption.Size = new Size(400, 18);
        lblCompanyCaption.TabIndex = 1;
        lblCompanyCaption.Text = "Current Company";
        // 
        // lblBranchCaption
        // 
        lblBranchCaption.Appearance.ForeColor = Color.Gray;
        lblBranchCaption.Appearance.Options.UseForeColor = true;
        lblBranchCaption.Dock = DockStyle.Fill;
        lblBranchCaption.Location = new Point(815, 11);
        lblBranchCaption.Name = "lblBranchCaption";
        lblBranchCaption.Size = new Size(400, 18);
        lblBranchCaption.TabIndex = 2;
        lblBranchCaption.Text = "Current Branch";
        // 
        // lblFiscalYearCaption
        // 
        lblFiscalYearCaption.Appearance.ForeColor = Color.Gray;
        lblFiscalYearCaption.Appearance.Options.UseForeColor = true;
        lblFiscalYearCaption.Dock = DockStyle.Fill;
        lblFiscalYearCaption.Location = new Point(1221, 11);
        lblFiscalYearCaption.Name = "lblFiscalYearCaption";
        lblFiscalYearCaption.Size = new Size(400, 18);
        lblFiscalYearCaption.TabIndex = 3;
        lblFiscalYearCaption.Text = "Current Fiscal Year";
        // 
        // lblCurrentUserCaption
        // 
        lblCurrentUserCaption.Appearance.ForeColor = Color.Gray;
        lblCurrentUserCaption.Appearance.Options.UseForeColor = true;
        lblCurrentUserCaption.Dock = DockStyle.Fill;
        lblCurrentUserCaption.Location = new Point(1627, 11);
        lblCurrentUserCaption.Name = "lblCurrentUserCaption";
        lblCurrentUserCaption.Size = new Size(400, 18);
        lblCurrentUserCaption.TabIndex = 4;
        lblCurrentUserCaption.Text = "Current User";
        // 
        // lblOrganizationValue
        // 
        lblOrganizationValue.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblOrganizationValue.Appearance.Options.UseFont = true;
        lblOrganizationValue.Dock = DockStyle.Fill;
        lblOrganizationValue.Location = new Point(3, 35);
        lblOrganizationValue.Name = "lblOrganizationValue";
        lblOrganizationValue.Size = new Size(400, 26);
        lblOrganizationValue.TabIndex = 5;
        lblOrganizationValue.Text = "Clovent Retail Group";
        // 
        // lblCompanyValue
        // 
        lblCompanyValue.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblCompanyValue.Appearance.Options.UseFont = true;
        lblCompanyValue.Dock = DockStyle.Fill;
        lblCompanyValue.Location = new Point(409, 35);
        lblCompanyValue.Name = "lblCompanyValue";
        lblCompanyValue.Size = new Size(400, 26);
        lblCompanyValue.TabIndex = 6;
        lblCompanyValue.Text = "Clovent Foods LLC";
        // 
        // lblBranchValue
        // 
        lblBranchValue.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblBranchValue.Appearance.Options.UseFont = true;
        lblBranchValue.Dock = DockStyle.Fill;
        lblBranchValue.Location = new Point(815, 35);
        lblBranchValue.Name = "lblBranchValue";
        lblBranchValue.Size = new Size(400, 26);
        lblBranchValue.TabIndex = 7;
        lblBranchValue.Text = "Downtown Branch";
        // 
        // lblFiscalYearValue
        // 
        lblFiscalYearValue.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblFiscalYearValue.Appearance.Options.UseFont = true;
        lblFiscalYearValue.Dock = DockStyle.Fill;
        lblFiscalYearValue.Location = new Point(1221, 35);
        lblFiscalYearValue.Name = "lblFiscalYearValue";
        lblFiscalYearValue.Size = new Size(400, 26);
        lblFiscalYearValue.TabIndex = 8;
        lblFiscalYearValue.Text = "FY2026 (Open)";
        // 
        // lblCurrentUserValue
        // 
        lblCurrentUserValue.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblCurrentUserValue.Appearance.Options.UseFont = true;
        lblCurrentUserValue.Dock = DockStyle.Fill;
        lblCurrentUserValue.Location = new Point(1627, 35);
        lblCurrentUserValue.Name = "lblCurrentUserValue";
        lblCurrentUserValue.Size = new Size(400, 26);
        lblCurrentUserValue.TabIndex = 9;
        lblCurrentUserValue.Text = "Nadeem Baig";
        // 
        // tlpKpi
        // 
        tlpKpi.ColumnCount = 4;
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpKpi.Controls.Add(pnlActiveSessions, 0, 0);
        tlpKpi.Controls.Add(pnlRecentLogins, 1, 0);
        tlpKpi.Controls.Add(pnlNotificationsCount, 2, 0);
        tlpKpi.Controls.Add(pnlTotalProducts, 3, 0);
        tlpKpi.Controls.Add(pnlLowStock, 0, 1);
        tlpKpi.Controls.Add(pnlOutOfStock, 1, 1);
        tlpKpi.Controls.Add(pnlInventoryValue, 2, 1);
        tlpKpi.Controls.Add(pnlTodaysSales, 3, 1);
        tlpKpi.Controls.Add(pnlOpenTables, 0, 2);
        tlpKpi.Controls.Add(pnlRunningOrders, 1, 2);
        tlpKpi.Controls.Add(pnlKitchenQueue, 2, 2);
        tlpKpi.Dock = DockStyle.Fill;
        tlpKpi.Location = new Point(8, 220);
        tlpKpi.Margin = new Padding(0, 0, 0, 8);
        tlpKpi.Name = "tlpKpi";
        tlpKpi.RowCount = 3;
        tlpKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        tlpKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        tlpKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        tlpKpi.Size = new Size(2030, 435);
        tlpKpi.TabIndex = 2;
        // 
        // pnlActiveSessions
        // 
        pnlActiveSessions.Controls.Add(lblActiveSessionsValue);
        pnlActiveSessions.Controls.Add(lblActiveSessionsCaption);
        pnlActiveSessions.Dock = DockStyle.Fill;
        pnlActiveSessions.Location = new Point(6, 6);
        pnlActiveSessions.Margin = new Padding(6);
        pnlActiveSessions.Name = "pnlActiveSessions";
        pnlActiveSessions.Size = new Size(495, 133);
        pnlActiveSessions.TabIndex = 0;
        // 
        // lblActiveSessionsValue
        // 
        lblActiveSessionsValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblActiveSessionsValue.Appearance.Options.UseFont = true;
        lblActiveSessionsValue.Appearance.Options.UseTextOptions = true;
        lblActiveSessionsValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblActiveSessionsValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblActiveSessionsValue.Dock = DockStyle.Fill;
        lblActiveSessionsValue.Location = new Point(3, 36);
        lblActiveSessionsValue.Name = "lblActiveSessionsValue";
        lblActiveSessionsValue.Size = new Size(39, 89);
        lblActiveSessionsValue.TabIndex = 1;
        lblActiveSessionsValue.Text = "3";
        // 
        // lblActiveSessionsCaption
        // 
        lblActiveSessionsCaption.Appearance.Options.UseTextOptions = true;
        lblActiveSessionsCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblActiveSessionsCaption.Dock = DockStyle.Top;
        lblActiveSessionsCaption.Location = new Point(3, 3);
        lblActiveSessionsCaption.Name = "lblActiveSessionsCaption";
        lblActiveSessionsCaption.Size = new Size(179, 33);
        lblActiveSessionsCaption.TabIndex = 0;
        lblActiveSessionsCaption.Text = "Active Sessions";
        // 
        // pnlRecentLogins
        // 
        pnlRecentLogins.Controls.Add(lblRecentLoginsValue);
        pnlRecentLogins.Controls.Add(lblRecentLoginsCaption);
        pnlRecentLogins.Dock = DockStyle.Fill;
        pnlRecentLogins.Location = new Point(513, 6);
        pnlRecentLogins.Margin = new Padding(6);
        pnlRecentLogins.Name = "pnlRecentLogins";
        pnlRecentLogins.Size = new Size(495, 133);
        pnlRecentLogins.TabIndex = 1;
        // 
        // lblRecentLoginsValue
        // 
        lblRecentLoginsValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblRecentLoginsValue.Appearance.Options.UseFont = true;
        lblRecentLoginsValue.Appearance.Options.UseTextOptions = true;
        lblRecentLoginsValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblRecentLoginsValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblRecentLoginsValue.Dock = DockStyle.Fill;
        lblRecentLoginsValue.Location = new Point(3, 36);
        lblRecentLoginsValue.Name = "lblRecentLoginsValue";
        lblRecentLoginsValue.Size = new Size(39, 89);
        lblRecentLoginsValue.TabIndex = 1;
        lblRecentLoginsValue.Text = "7";
        // 
        // lblRecentLoginsCaption
        // 
        lblRecentLoginsCaption.Appearance.Options.UseTextOptions = true;
        lblRecentLoginsCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblRecentLoginsCaption.Dock = DockStyle.Top;
        lblRecentLoginsCaption.Location = new Point(3, 3);
        lblRecentLoginsCaption.Name = "lblRecentLoginsCaption";
        lblRecentLoginsCaption.Size = new Size(181, 33);
        lblRecentLoginsCaption.TabIndex = 0;
        lblRecentLoginsCaption.Text = "Logins (7 days)";
        // 
        // pnlNotificationsCount
        // 
        pnlNotificationsCount.Controls.Add(lblNotificationsCountValue);
        pnlNotificationsCount.Controls.Add(lblNotificationsCountCaption);
        pnlNotificationsCount.Dock = DockStyle.Fill;
        pnlNotificationsCount.Location = new Point(1020, 6);
        pnlNotificationsCount.Margin = new Padding(6);
        pnlNotificationsCount.Name = "pnlNotificationsCount";
        pnlNotificationsCount.Size = new Size(495, 133);
        pnlNotificationsCount.TabIndex = 2;
        // 
        // lblNotificationsCountValue
        // 
        lblNotificationsCountValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblNotificationsCountValue.Appearance.Options.UseFont = true;
        lblNotificationsCountValue.Appearance.Options.UseTextOptions = true;
        lblNotificationsCountValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblNotificationsCountValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblNotificationsCountValue.Dock = DockStyle.Fill;
        lblNotificationsCountValue.Location = new Point(3, 36);
        lblNotificationsCountValue.Name = "lblNotificationsCountValue";
        lblNotificationsCountValue.Size = new Size(39, 89);
        lblNotificationsCountValue.TabIndex = 1;
        lblNotificationsCountValue.Text = "2";
        // 
        // lblNotificationsCountCaption
        // 
        lblNotificationsCountCaption.Appearance.Options.UseTextOptions = true;
        lblNotificationsCountCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblNotificationsCountCaption.Dock = DockStyle.Top;
        lblNotificationsCountCaption.Location = new Point(3, 3);
        lblNotificationsCountCaption.Name = "lblNotificationsCountCaption";
        lblNotificationsCountCaption.Size = new Size(146, 33);
        lblNotificationsCountCaption.TabIndex = 0;
        lblNotificationsCountCaption.Text = "Notifications";
        // 
        // pnlTotalProducts
        // 
        pnlTotalProducts.Controls.Add(lblTotalProductsValue);
        pnlTotalProducts.Controls.Add(lblTotalProductsCaption);
        pnlTotalProducts.Dock = DockStyle.Fill;
        pnlTotalProducts.Location = new Point(1527, 6);
        pnlTotalProducts.Margin = new Padding(6);
        pnlTotalProducts.Name = "pnlTotalProducts";
        pnlTotalProducts.Size = new Size(497, 133);
        pnlTotalProducts.TabIndex = 3;
        // 
        // lblTotalProductsValue
        // 
        lblTotalProductsValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblTotalProductsValue.Appearance.Options.UseFont = true;
        lblTotalProductsValue.Appearance.Options.UseTextOptions = true;
        lblTotalProductsValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblTotalProductsValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblTotalProductsValue.Dock = DockStyle.Fill;
        lblTotalProductsValue.Location = new Point(3, 36);
        lblTotalProductsValue.Name = "lblTotalProductsValue";
        lblTotalProductsValue.Size = new Size(39, 89);
        lblTotalProductsValue.TabIndex = 1;
        lblTotalProductsValue.Text = "128";
        // 
        // lblTotalProductsCaption
        // 
        lblTotalProductsCaption.Appearance.Options.UseTextOptions = true;
        lblTotalProductsCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblTotalProductsCaption.Dock = DockStyle.Top;
        lblTotalProductsCaption.Location = new Point(3, 3);
        lblTotalProductsCaption.Name = "lblTotalProductsCaption";
        lblTotalProductsCaption.Size = new Size(171, 33);
        lblTotalProductsCaption.TabIndex = 0;
        lblTotalProductsCaption.Text = "Total Products";
        // 
        // pnlLowStock
        // 
        pnlLowStock.Controls.Add(lblLowStockValue);
        pnlLowStock.Controls.Add(lblLowStockCaption);
        pnlLowStock.Dock = DockStyle.Fill;
        pnlLowStock.Location = new Point(6, 151);
        pnlLowStock.Margin = new Padding(6);
        pnlLowStock.Name = "pnlLowStock";
        pnlLowStock.Size = new Size(495, 133);
        pnlLowStock.TabIndex = 4;
        // 
        // lblLowStockValue
        // 
        lblLowStockValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblLowStockValue.Appearance.Options.UseFont = true;
        lblLowStockValue.Appearance.Options.UseTextOptions = true;
        lblLowStockValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblLowStockValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblLowStockValue.Dock = DockStyle.Fill;
        lblLowStockValue.Location = new Point(3, 36);
        lblLowStockValue.Name = "lblLowStockValue";
        lblLowStockValue.Size = new Size(39, 89);
        lblLowStockValue.TabIndex = 1;
        lblLowStockValue.Text = "5";
        // 
        // lblLowStockCaption
        // 
        lblLowStockCaption.Appearance.Options.UseTextOptions = true;
        lblLowStockCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblLowStockCaption.Dock = DockStyle.Top;
        lblLowStockCaption.Location = new Point(3, 3);
        lblLowStockCaption.Name = "lblLowStockCaption";
        lblLowStockCaption.Size = new Size(121, 33);
        lblLowStockCaption.TabIndex = 0;
        lblLowStockCaption.Text = "Low Stock";
        // 
        // pnlOutOfStock
        // 
        pnlOutOfStock.Controls.Add(lblOutOfStockValue);
        pnlOutOfStock.Controls.Add(lblOutOfStockCaption);
        pnlOutOfStock.Dock = DockStyle.Fill;
        pnlOutOfStock.Location = new Point(513, 151);
        pnlOutOfStock.Margin = new Padding(6);
        pnlOutOfStock.Name = "pnlOutOfStock";
        pnlOutOfStock.Size = new Size(495, 133);
        pnlOutOfStock.TabIndex = 5;
        // 
        // lblOutOfStockValue
        // 
        lblOutOfStockValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblOutOfStockValue.Appearance.Options.UseFont = true;
        lblOutOfStockValue.Appearance.Options.UseTextOptions = true;
        lblOutOfStockValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblOutOfStockValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblOutOfStockValue.Dock = DockStyle.Fill;
        lblOutOfStockValue.Location = new Point(3, 36);
        lblOutOfStockValue.Name = "lblOutOfStockValue";
        lblOutOfStockValue.Size = new Size(39, 89);
        lblOutOfStockValue.TabIndex = 1;
        lblOutOfStockValue.Text = "1";
        // 
        // lblOutOfStockCaption
        // 
        lblOutOfStockCaption.Appearance.Options.UseTextOptions = true;
        lblOutOfStockCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblOutOfStockCaption.Dock = DockStyle.Top;
        lblOutOfStockCaption.Location = new Point(3, 3);
        lblOutOfStockCaption.Name = "lblOutOfStockCaption";
        lblOutOfStockCaption.Size = new Size(148, 33);
        lblOutOfStockCaption.TabIndex = 0;
        lblOutOfStockCaption.Text = "Out of Stock";
        // 
        // pnlInventoryValue
        // 
        pnlInventoryValue.Controls.Add(lblInventoryValueValue);
        pnlInventoryValue.Controls.Add(lblInventoryValueCaption);
        pnlInventoryValue.Dock = DockStyle.Fill;
        pnlInventoryValue.Location = new Point(1020, 151);
        pnlInventoryValue.Margin = new Padding(6);
        pnlInventoryValue.Name = "pnlInventoryValue";
        pnlInventoryValue.Size = new Size(495, 133);
        pnlInventoryValue.TabIndex = 6;
        // 
        // lblInventoryValueValue
        // 
        lblInventoryValueValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblInventoryValueValue.Appearance.Options.UseFont = true;
        lblInventoryValueValue.Appearance.Options.UseTextOptions = true;
        lblInventoryValueValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblInventoryValueValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblInventoryValueValue.Dock = DockStyle.Fill;
        lblInventoryValueValue.Location = new Point(3, 36);
        lblInventoryValueValue.Name = "lblInventoryValueValue";
        lblInventoryValueValue.Size = new Size(39, 89);
        lblInventoryValueValue.TabIndex = 1;
        lblInventoryValueValue.Text = "45,230.00";
        // 
        // lblInventoryValueCaption
        // 
        lblInventoryValueCaption.Appearance.Options.UseTextOptions = true;
        lblInventoryValueCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblInventoryValueCaption.Dock = DockStyle.Top;
        lblInventoryValueCaption.Location = new Point(3, 3);
        lblInventoryValueCaption.Name = "lblInventoryValueCaption";
        lblInventoryValueCaption.Size = new Size(187, 33);
        lblInventoryValueCaption.TabIndex = 0;
        lblInventoryValueCaption.Text = "Inventory Value";
        // 
        // pnlTodaysSales
        // 
        pnlTodaysSales.Controls.Add(lblTodaysSalesValue);
        pnlTodaysSales.Controls.Add(lblTodaysSalesCaption);
        pnlTodaysSales.Dock = DockStyle.Fill;
        pnlTodaysSales.Location = new Point(1527, 151);
        pnlTodaysSales.Margin = new Padding(6);
        pnlTodaysSales.Name = "pnlTodaysSales";
        pnlTodaysSales.Size = new Size(497, 133);
        pnlTodaysSales.TabIndex = 7;
        // 
        // lblTodaysSalesValue
        // 
        lblTodaysSalesValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblTodaysSalesValue.Appearance.Options.UseFont = true;
        lblTodaysSalesValue.Appearance.Options.UseTextOptions = true;
        lblTodaysSalesValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblTodaysSalesValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblTodaysSalesValue.Dock = DockStyle.Fill;
        lblTodaysSalesValue.Location = new Point(3, 36);
        lblTodaysSalesValue.Name = "lblTodaysSalesValue";
        lblTodaysSalesValue.Size = new Size(39, 89);
        lblTodaysSalesValue.TabIndex = 1;
        lblTodaysSalesValue.Text = "3,412.50";
        // 
        // lblTodaysSalesCaption
        // 
        lblTodaysSalesCaption.Appearance.Options.UseTextOptions = true;
        lblTodaysSalesCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblTodaysSalesCaption.Dock = DockStyle.Top;
        lblTodaysSalesCaption.Location = new Point(3, 3);
        lblTodaysSalesCaption.Name = "lblTodaysSalesCaption";
        lblTodaysSalesCaption.Size = new Size(160, 33);
        lblTodaysSalesCaption.TabIndex = 0;
        lblTodaysSalesCaption.Text = "Today's Sales";
        // 
        // pnlOpenTables
        // 
        pnlOpenTables.Controls.Add(lblOpenTablesValue);
        pnlOpenTables.Controls.Add(lblOpenTablesCaption);
        pnlOpenTables.Dock = DockStyle.Fill;
        pnlOpenTables.Location = new Point(6, 296);
        pnlOpenTables.Margin = new Padding(6);
        pnlOpenTables.Name = "pnlOpenTables";
        pnlOpenTables.Size = new Size(495, 133);
        pnlOpenTables.TabIndex = 8;
        // 
        // lblOpenTablesValue
        // 
        lblOpenTablesValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblOpenTablesValue.Appearance.Options.UseFont = true;
        lblOpenTablesValue.Appearance.Options.UseTextOptions = true;
        lblOpenTablesValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblOpenTablesValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblOpenTablesValue.Dock = DockStyle.Fill;
        lblOpenTablesValue.Location = new Point(3, 36);
        lblOpenTablesValue.Name = "lblOpenTablesValue";
        lblOpenTablesValue.Size = new Size(39, 89);
        lblOpenTablesValue.TabIndex = 1;
        lblOpenTablesValue.Text = "6";
        // 
        // lblOpenTablesCaption
        // 
        lblOpenTablesCaption.Appearance.Options.UseTextOptions = true;
        lblOpenTablesCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblOpenTablesCaption.Dock = DockStyle.Top;
        lblOpenTablesCaption.Location = new Point(3, 3);
        lblOpenTablesCaption.Name = "lblOpenTablesCaption";
        lblOpenTablesCaption.Size = new Size(148, 33);
        lblOpenTablesCaption.TabIndex = 0;
        lblOpenTablesCaption.Text = "Open Tables";
        // 
        // pnlRunningOrders
        // 
        pnlRunningOrders.Controls.Add(lblRunningOrdersValue);
        pnlRunningOrders.Controls.Add(lblRunningOrdersCaption);
        pnlRunningOrders.Dock = DockStyle.Fill;
        pnlRunningOrders.Location = new Point(513, 296);
        pnlRunningOrders.Margin = new Padding(6);
        pnlRunningOrders.Name = "pnlRunningOrders";
        pnlRunningOrders.Size = new Size(495, 133);
        pnlRunningOrders.TabIndex = 9;
        // 
        // lblRunningOrdersValue
        // 
        lblRunningOrdersValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblRunningOrdersValue.Appearance.Options.UseFont = true;
        lblRunningOrdersValue.Appearance.Options.UseTextOptions = true;
        lblRunningOrdersValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblRunningOrdersValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblRunningOrdersValue.Dock = DockStyle.Fill;
        lblRunningOrdersValue.Location = new Point(3, 36);
        lblRunningOrdersValue.Name = "lblRunningOrdersValue";
        lblRunningOrdersValue.Size = new Size(39, 89);
        lblRunningOrdersValue.TabIndex = 1;
        lblRunningOrdersValue.Text = "4";
        // 
        // lblRunningOrdersCaption
        // 
        lblRunningOrdersCaption.Appearance.Options.UseTextOptions = true;
        lblRunningOrdersCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblRunningOrdersCaption.Dock = DockStyle.Top;
        lblRunningOrdersCaption.Location = new Point(3, 3);
        lblRunningOrdersCaption.Name = "lblRunningOrdersCaption";
        lblRunningOrdersCaption.Size = new Size(186, 33);
        lblRunningOrdersCaption.TabIndex = 0;
        lblRunningOrdersCaption.Text = "Running Orders";
        // 
        // pnlKitchenQueue
        // 
        pnlKitchenQueue.Controls.Add(lblKitchenQueueValue);
        pnlKitchenQueue.Controls.Add(lblKitchenQueueCaption);
        pnlKitchenQueue.Dock = DockStyle.Fill;
        pnlKitchenQueue.Location = new Point(1020, 296);
        pnlKitchenQueue.Margin = new Padding(6);
        pnlKitchenQueue.Name = "pnlKitchenQueue";
        pnlKitchenQueue.Size = new Size(495, 133);
        pnlKitchenQueue.TabIndex = 10;
        // 
        // lblKitchenQueueValue
        // 
        lblKitchenQueueValue.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblKitchenQueueValue.Appearance.Options.UseFont = true;
        lblKitchenQueueValue.Appearance.Options.UseTextOptions = true;
        lblKitchenQueueValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblKitchenQueueValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblKitchenQueueValue.Dock = DockStyle.Fill;
        lblKitchenQueueValue.Location = new Point(3, 36);
        lblKitchenQueueValue.Name = "lblKitchenQueueValue";
        lblKitchenQueueValue.Size = new Size(39, 89);
        lblKitchenQueueValue.TabIndex = 1;
        lblKitchenQueueValue.Text = "2";
        // 
        // lblKitchenQueueCaption
        // 
        lblKitchenQueueCaption.Appearance.Options.UseTextOptions = true;
        lblKitchenQueueCaption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblKitchenQueueCaption.Dock = DockStyle.Top;
        lblKitchenQueueCaption.Location = new Point(3, 3);
        lblKitchenQueueCaption.Name = "lblKitchenQueueCaption";
        lblKitchenQueueCaption.Size = new Size(172, 33);
        lblKitchenQueueCaption.TabIndex = 0;
        lblKitchenQueueCaption.Text = "Kitchen Queue";
        // 
        // tlpLists
        // 
        tlpLists.ColumnCount = 4;
        tlpLists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpLists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpLists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpLists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpLists.Controls.Add(pnlRecentActivity, 0, 0);
        tlpLists.Controls.Add(pnlNotificationsList, 1, 0);
        tlpLists.Controls.Add(pnlStockMovements, 2, 0);
        tlpLists.Controls.Add(pnlTopSellingItems, 3, 0);
        tlpLists.Dock = DockStyle.Fill;
        tlpLists.Location = new Point(11, 666);
        tlpLists.Name = "tlpLists";
        tlpLists.RowCount = 1;
        tlpLists.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpLists.Size = new Size(2024, 740);
        tlpLists.TabIndex = 3;
        // 
        // pnlRecentActivity
        // 
        pnlRecentActivity.Controls.Add(lstRecentActivity);
        pnlRecentActivity.Controls.Add(lblRecentActivityCaption);
        pnlRecentActivity.Dock = DockStyle.Fill;
        pnlRecentActivity.Location = new Point(6, 6);
        pnlRecentActivity.Margin = new Padding(6);
        pnlRecentActivity.Name = "pnlRecentActivity";
        pnlRecentActivity.Size = new Size(494, 728);
        pnlRecentActivity.TabIndex = 0;
        // 
        // lstRecentActivity
        // 
        lstRecentActivity.Dock = DockStyle.Fill;
        lstRecentActivity.Location = new Point(3, 53);
        lstRecentActivity.Name = "lstRecentActivity";
        lstRecentActivity.Size = new Size(488, 672);
        lstRecentActivity.TabIndex = 1;
        // Sample rows so the Visual Studio Designer canvas never renders an
        // empty list - RefreshAsync (DashboardView.cs) replaces these with
        // real recent-login-attempt rows the moment this screen actually
        // runs; the Designer never calls RefreshAsync.
        lstRecentActivity.Items.AddRange(new object[] {
            "08/08/2026 09:14 AM  -  Success",
            "08/08/2026 08:02 AM  -  Success",
            "08/07/2026 06:47 PM  -  Failed" });
        // 
        // lblRecentActivityCaption
        // 
        lblRecentActivityCaption.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblRecentActivityCaption.Appearance.Options.UseFont = true;
        lblRecentActivityCaption.Dock = DockStyle.Top;
        lblRecentActivityCaption.Location = new Point(3, 3);
        lblRecentActivityCaption.Name = "lblRecentActivityCaption";
        lblRecentActivityCaption.Size = new Size(263, 50);
        lblRecentActivityCaption.TabIndex = 0;
        lblRecentActivityCaption.Text = "Recent Activity";
        // 
        // pnlNotificationsList
        // 
        pnlNotificationsList.Controls.Add(lstNotifications);
        pnlNotificationsList.Controls.Add(lblNotificationsListCaption);
        pnlNotificationsList.Dock = DockStyle.Fill;
        pnlNotificationsList.Location = new Point(512, 6);
        pnlNotificationsList.Margin = new Padding(6);
        pnlNotificationsList.Name = "pnlNotificationsList";
        pnlNotificationsList.Size = new Size(494, 728);
        pnlNotificationsList.TabIndex = 1;
        // 
        // lstNotifications
        // 
        lstNotifications.Dock = DockStyle.Fill;
        lstNotifications.Location = new Point(3, 53);
        lstNotifications.Name = "lstNotifications";
        lstNotifications.Size = new Size(488, 672);
        lstNotifications.TabIndex = 1;
        // Sample rows - see lstRecentActivity's comment above.
        lstNotifications.Items.AddRange(new object[] {
            "08/08/2026 09:20 AM  -  Low Stock: Espresso Beans is low on stock",
            "08/07/2026 05:10 PM  -  Shift Closed: End-of-day report is ready" });
        // 
        // lblNotificationsListCaption
        // 
        lblNotificationsListCaption.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblNotificationsListCaption.Appearance.Options.UseFont = true;
        lblNotificationsListCaption.Dock = DockStyle.Top;
        lblNotificationsListCaption.Location = new Point(3, 3);
        lblNotificationsListCaption.Name = "lblNotificationsListCaption";
        lblNotificationsListCaption.Size = new Size(226, 50);
        lblNotificationsListCaption.TabIndex = 0;
        lblNotificationsListCaption.Text = "Notifications";
        // 
        // pnlStockMovements
        // 
        pnlStockMovements.Controls.Add(lstStockMovements);
        pnlStockMovements.Controls.Add(lblStockMovementsCaption);
        pnlStockMovements.Dock = DockStyle.Fill;
        pnlStockMovements.Location = new Point(1018, 6);
        pnlStockMovements.Margin = new Padding(6);
        pnlStockMovements.Name = "pnlStockMovements";
        pnlStockMovements.Size = new Size(494, 728);
        pnlStockMovements.TabIndex = 2;
        // 
        // lstStockMovements
        // 
        lstStockMovements.Dock = DockStyle.Fill;
        lstStockMovements.Location = new Point(3, 53);
        lstStockMovements.Name = "lstStockMovements";
        lstStockMovements.Size = new Size(488, 672);
        lstStockMovements.TabIndex = 1;
        // Sample rows - see lstRecentActivity's comment above.
        lstStockMovements.Items.AddRange(new object[] {
            "08/08/2026 08:45 AM  -  Receive 50",
            "08/07/2026 07:30 PM  -  Sale -12",
            "08/07/2026 02:15 PM  -  Adjustment -2" });
        // 
        // lblStockMovementsCaption
        // 
        lblStockMovementsCaption.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblStockMovementsCaption.Appearance.Options.UseFont = true;
        lblStockMovementsCaption.Dock = DockStyle.Top;
        lblStockMovementsCaption.Location = new Point(3, 3);
        lblStockMovementsCaption.Name = "lblStockMovementsCaption";
        lblStockMovementsCaption.Size = new Size(311, 50);
        lblStockMovementsCaption.TabIndex = 0;
        lblStockMovementsCaption.Text = "Stock Movements";
        // 
        // pnlTopSellingItems
        // 
        pnlTopSellingItems.Controls.Add(lstTopSellingItems);
        pnlTopSellingItems.Controls.Add(lblTopSellingItemsCaption);
        pnlTopSellingItems.Dock = DockStyle.Fill;
        pnlTopSellingItems.Location = new Point(1524, 6);
        pnlTopSellingItems.Margin = new Padding(6);
        pnlTopSellingItems.Name = "pnlTopSellingItems";
        pnlTopSellingItems.Size = new Size(494, 728);
        pnlTopSellingItems.TabIndex = 3;
        // 
        // lstTopSellingItems
        // 
        lstTopSellingItems.Dock = DockStyle.Fill;
        lstTopSellingItems.Location = new Point(3, 53);
        lstTopSellingItems.Name = "lstTopSellingItems";
        lstTopSellingItems.Size = new Size(488, 672);
        lstTopSellingItems.TabIndex = 1;
        // Sample rows - see lstRecentActivity's comment above.
        lstTopSellingItems.Items.AddRange(new object[] {
            "BEV-001 Espresso  -  42.00 sold",
            "FOOD-014 Chicken Burger  -  28.00 sold",
            "BEV-007 Iced Latte  -  19.00 sold" });
        // 
        // lblTopSellingItemsCaption
        // 
        lblTopSellingItemsCaption.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblTopSellingItemsCaption.Appearance.Options.UseFont = true;
        lblTopSellingItemsCaption.Dock = DockStyle.Top;
        lblTopSellingItemsCaption.Location = new Point(3, 3);
        lblTopSellingItemsCaption.Name = "lblTopSellingItemsCaption";
        lblTopSellingItemsCaption.Size = new Size(303, 50);
        lblTopSellingItemsCaption.TabIndex = 0;
        lblTopSellingItemsCaption.Text = "Top Selling Items";
        // 
        // flowBottom
        // 
        flowBottom.AutoSize = true;
        flowBottom.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flowBottom.Controls.Add(lblCompanySelectorCaption);
        flowBottom.Controls.Add(cmbCompany);
        flowBottom.Controls.Add(lblBranchSelectorCaption);
        flowBottom.Controls.Add(cmbBranch);
        flowBottom.Controls.Add(btnRefresh);
        flowBottom.Controls.Add(btnViewNotifications);
        flowBottom.Dock = DockStyle.Fill;
        flowBottom.Location = new Point(11, 1412);
        flowBottom.Name = "flowBottom";
        flowBottom.Padding = new Padding(0, 8, 0, 0);
        flowBottom.Size = new Size(2024, 70);
        flowBottom.TabIndex = 4;
        // 
        // lblCompanySelectorCaption
        // 
        lblCompanySelectorCaption.Location = new Point(0, 16);
        lblCompanySelectorCaption.Margin = new Padding(0, 8, 4, 0);
        lblCompanySelectorCaption.Name = "lblCompanySelectorCaption";
        lblCompanySelectorCaption.Size = new Size(121, 33);
        lblCompanySelectorCaption.TabIndex = 0;
        lblCompanySelectorCaption.Text = "Company:";
        // 
        // cmbCompany
        // 
        cmbCompany.Location = new Point(125, 12);
        cmbCompany.Margin = new Padding(0, 4, 8, 4);
        cmbCompany.Name = "cmbCompany";
        cmbCompany.Size = new Size(220, 48);
        cmbCompany.TabIndex = 1;
        cmbCompany.SelectedIndexChanged += CmbCompany_SelectedIndexChanged;
        // 
        // lblBranchSelectorCaption
        // 
        lblBranchSelectorCaption.Location = new Point(353, 16);
        lblBranchSelectorCaption.Margin = new Padding(0, 8, 4, 0);
        lblBranchSelectorCaption.Name = "lblBranchSelectorCaption";
        lblBranchSelectorCaption.Size = new Size(92, 33);
        lblBranchSelectorCaption.TabIndex = 2;
        lblBranchSelectorCaption.Text = "Branch:";
        // 
        // cmbBranch
        // 
        cmbBranch.Location = new Point(449, 12);
        cmbBranch.Margin = new Padding(0, 4, 8, 4);
        cmbBranch.Name = "cmbBranch";
        cmbBranch.Size = new Size(220, 48);
        cmbBranch.TabIndex = 3;
        cmbBranch.SelectedIndexChanged += CmbBranch_SelectedIndexChanged;
        // 
        // btnRefresh
        // 
        btnRefresh.AutoSize = true;
        btnRefresh.Location = new Point(685, 12);
        btnRefresh.Margin = new Padding(8, 4, 4, 4);
        btnRefresh.MinimumSize = new Size(80, 30);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(106, 54);
        btnRefresh.TabIndex = 4;
        btnRefresh.Text = "Refresh";
        btnRefresh.Click += BtnRefresh_Click;
        // 
        // btnViewNotifications
        // 
        btnViewNotifications.AutoSize = true;
        btnViewNotifications.Location = new Point(799, 12);
        btnViewNotifications.Margin = new Padding(4);
        btnViewNotifications.MinimumSize = new Size(130, 30);
        btnViewNotifications.Name = "btnViewNotifications";
        btnViewNotifications.Size = new Size(261, 54);
        btnViewNotifications.TabIndex = 5;
        btnViewNotifications.Text = "View All Notifications";
        btnViewNotifications.Click += BtnViewNotifications_Click;
        // 
        // DashboardView
        // 
        Name = "DashboardView";
        Size = new Size(2052, 1499);
        ((System.ComponentModel.ISupportInitialize)ToolbarPanel).EndInit();
        ((System.ComponentModel.ISupportInitialize)ContentPanel).EndInit();
        ContentPanel.ResumeLayout(false);
        scrollHost.ResumeLayout(false);
        tlpMain.ResumeLayout(false);
        tlpMain.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlTitleBar).EndInit();
        pnlTitleBar.ResumeLayout(false);
        pnlTitleBar.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)prgLoading.Properties).EndInit();
        tlpContext.ResumeLayout(false);
        tlpContext.PerformLayout();
        tlpKpi.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)pnlActiveSessions).EndInit();
        pnlActiveSessions.ResumeLayout(false);
        pnlActiveSessions.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlRecentLogins).EndInit();
        pnlRecentLogins.ResumeLayout(false);
        pnlRecentLogins.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlNotificationsCount).EndInit();
        pnlNotificationsCount.ResumeLayout(false);
        pnlNotificationsCount.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlTotalProducts).EndInit();
        pnlTotalProducts.ResumeLayout(false);
        pnlTotalProducts.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlLowStock).EndInit();
        pnlLowStock.ResumeLayout(false);
        pnlLowStock.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlOutOfStock).EndInit();
        pnlOutOfStock.ResumeLayout(false);
        pnlOutOfStock.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlInventoryValue).EndInit();
        pnlInventoryValue.ResumeLayout(false);
        pnlInventoryValue.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlTodaysSales).EndInit();
        pnlTodaysSales.ResumeLayout(false);
        pnlTodaysSales.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlOpenTables).EndInit();
        pnlOpenTables.ResumeLayout(false);
        pnlOpenTables.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlRunningOrders).EndInit();
        pnlRunningOrders.ResumeLayout(false);
        pnlRunningOrders.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pnlKitchenQueue).EndInit();
        pnlKitchenQueue.ResumeLayout(false);
        pnlKitchenQueue.PerformLayout();
        tlpLists.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)pnlRecentActivity).EndInit();
        pnlRecentActivity.ResumeLayout(false);
        pnlRecentActivity.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)lstRecentActivity).EndInit();
        ((System.ComponentModel.ISupportInitialize)pnlNotificationsList).EndInit();
        pnlNotificationsList.ResumeLayout(false);
        pnlNotificationsList.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)lstNotifications).EndInit();
        ((System.ComponentModel.ISupportInitialize)pnlStockMovements).EndInit();
        pnlStockMovements.ResumeLayout(false);
        pnlStockMovements.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)lstStockMovements).EndInit();
        ((System.ComponentModel.ISupportInitialize)pnlTopSellingItems).EndInit();
        pnlTopSellingItems.ResumeLayout(false);
        pnlTopSellingItems.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)lstTopSellingItems).EndInit();
        flowBottom.ResumeLayout(false);
        flowBottom.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)cmbCompany.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)cmbBranch.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private Panel scrollHost;
    private TableLayoutPanel tlpMain;
    private DevExpress.XtraEditors.PanelControl pnlTitleBar;
    private DevExpress.XtraEditors.LabelControl lblDashboardTitle;
    private DevExpress.XtraEditors.ProgressBarControl prgLoading;
    private TableLayoutPanel tlpContext;
    private DevExpress.XtraEditors.LabelControl lblOrganizationCaption;
    private DevExpress.XtraEditors.LabelControl lblOrganizationValue;
    private DevExpress.XtraEditors.LabelControl lblCompanyCaption;
    private DevExpress.XtraEditors.LabelControl lblCompanyValue;
    private DevExpress.XtraEditors.LabelControl lblBranchCaption;
    private DevExpress.XtraEditors.LabelControl lblBranchValue;
    private DevExpress.XtraEditors.LabelControl lblFiscalYearCaption;
    private DevExpress.XtraEditors.LabelControl lblFiscalYearValue;
    private DevExpress.XtraEditors.LabelControl lblCurrentUserCaption;
    private DevExpress.XtraEditors.LabelControl lblCurrentUserValue;
    private TableLayoutPanel tlpKpi;
    private DevExpress.XtraEditors.PanelControl pnlActiveSessions;
    private DevExpress.XtraEditors.LabelControl lblActiveSessionsCaption;
    private DevExpress.XtraEditors.LabelControl lblActiveSessionsValue;
    private DevExpress.XtraEditors.PanelControl pnlRecentLogins;
    private DevExpress.XtraEditors.LabelControl lblRecentLoginsCaption;
    private DevExpress.XtraEditors.LabelControl lblRecentLoginsValue;
    private DevExpress.XtraEditors.PanelControl pnlNotificationsCount;
    private DevExpress.XtraEditors.LabelControl lblNotificationsCountCaption;
    private DevExpress.XtraEditors.LabelControl lblNotificationsCountValue;
    private DevExpress.XtraEditors.PanelControl pnlTotalProducts;
    private DevExpress.XtraEditors.LabelControl lblTotalProductsCaption;
    private DevExpress.XtraEditors.LabelControl lblTotalProductsValue;
    private DevExpress.XtraEditors.PanelControl pnlLowStock;
    private DevExpress.XtraEditors.LabelControl lblLowStockCaption;
    private DevExpress.XtraEditors.LabelControl lblLowStockValue;
    private DevExpress.XtraEditors.PanelControl pnlOutOfStock;
    private DevExpress.XtraEditors.LabelControl lblOutOfStockCaption;
    private DevExpress.XtraEditors.LabelControl lblOutOfStockValue;
    private DevExpress.XtraEditors.PanelControl pnlInventoryValue;
    private DevExpress.XtraEditors.LabelControl lblInventoryValueCaption;
    private DevExpress.XtraEditors.LabelControl lblInventoryValueValue;
    private DevExpress.XtraEditors.PanelControl pnlTodaysSales;
    private DevExpress.XtraEditors.LabelControl lblTodaysSalesCaption;
    private DevExpress.XtraEditors.LabelControl lblTodaysSalesValue;
    private DevExpress.XtraEditors.PanelControl pnlOpenTables;
    private DevExpress.XtraEditors.LabelControl lblOpenTablesCaption;
    private DevExpress.XtraEditors.LabelControl lblOpenTablesValue;
    private DevExpress.XtraEditors.PanelControl pnlRunningOrders;
    private DevExpress.XtraEditors.LabelControl lblRunningOrdersCaption;
    private DevExpress.XtraEditors.LabelControl lblRunningOrdersValue;
    private DevExpress.XtraEditors.PanelControl pnlKitchenQueue;
    private DevExpress.XtraEditors.LabelControl lblKitchenQueueCaption;
    private DevExpress.XtraEditors.LabelControl lblKitchenQueueValue;
    private TableLayoutPanel tlpLists;
    private DevExpress.XtraEditors.PanelControl pnlRecentActivity;
    private DevExpress.XtraEditors.LabelControl lblRecentActivityCaption;
    private DevExpress.XtraEditors.ListBoxControl lstRecentActivity;
    private DevExpress.XtraEditors.PanelControl pnlNotificationsList;
    private DevExpress.XtraEditors.LabelControl lblNotificationsListCaption;
    private DevExpress.XtraEditors.ListBoxControl lstNotifications;
    private DevExpress.XtraEditors.PanelControl pnlStockMovements;
    private DevExpress.XtraEditors.LabelControl lblStockMovementsCaption;
    private DevExpress.XtraEditors.ListBoxControl lstStockMovements;
    private DevExpress.XtraEditors.PanelControl pnlTopSellingItems;
    private DevExpress.XtraEditors.LabelControl lblTopSellingItemsCaption;
    private DevExpress.XtraEditors.ListBoxControl lstTopSellingItems;
    private FlowLayoutPanel flowBottom;
    private DevExpress.XtraEditors.LabelControl lblCompanySelectorCaption;
    private DevExpress.XtraEditors.ComboBoxEdit cmbCompany;
    private DevExpress.XtraEditors.LabelControl lblBranchSelectorCaption;
    private DevExpress.XtraEditors.ComboBoxEdit cmbBranch;
    private DevExpress.XtraEditors.SimpleButton btnRefresh;
    private DevExpress.XtraEditors.SimpleButton btnViewNotifications;
}
