namespace Clovent.Desktop.Forms.Settings;

partial class PrinterSettingsForm
{
    private System.ComponentModel.IContainer components = null;

    /// <inheritdoc/>
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
        this.lblTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblSubtitle = new DevExpress.XtraEditors.LabelControl();
        this.grpInstalled = new DevExpress.XtraEditors.GroupControl();
        this.lblInstalled = new DevExpress.XtraEditors.LabelControl();
        this.cmbInstalledPrinters = new DevExpress.XtraEditors.ComboBoxEdit();
        this.btnRefreshQueues = new DevExpress.XtraEditors.SimpleButton();
        this.lblCapabilityInfo = new DevExpress.XtraEditors.LabelControl();
        this.tabControl = new DevExpress.XtraTab.XtraTabControl();
        this.tabProfiles = new DevExpress.XtraTab.XtraTabPage();
        this.grpProfiles = new DevExpress.XtraEditors.GroupControl();
        this.lstProfiles = new DevExpress.XtraEditors.ListBoxControl();
        this.btnAddProfile = new DevExpress.XtraEditors.SimpleButton();
        this.btnDeleteProfile = new DevExpress.XtraEditors.SimpleButton();
        this.btnSetDefault = new DevExpress.XtraEditors.SimpleButton();
        this.btnSetTerminalDefault = new DevExpress.XtraEditors.SimpleButton();
        this.grpProfileDetails = new DevExpress.XtraEditors.GroupControl();
        this.lblProfileName = new DevExpress.XtraEditors.LabelControl();
        this.txtProfileName = new DevExpress.XtraEditors.TextEdit();
        this.lblQueue = new DevExpress.XtraEditors.LabelControl();
        this.txtQueueName = new DevExpress.XtraEditors.TextEdit();
        this.lblRole = new DevExpress.XtraEditors.LabelControl();
        this.cmbRole = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblPaperWidth = new DevExpress.XtraEditors.LabelControl();
        this.cmbPaperWidth = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblColumns = new DevExpress.XtraEditors.LabelControl();
        this.spinColumns = new DevExpress.XtraEditors.SpinEdit();
        this.lblCopies = new DevExpress.XtraEditors.LabelControl();
        this.spinCopies = new DevExpress.XtraEditors.SpinEdit();
        this.chkCutter = new DevExpress.XtraEditors.CheckEdit();
        this.chkDrawer = new DevExpress.XtraEditors.CheckEdit();
        this.lblHardwareNotes = new DevExpress.XtraEditors.LabelControl();
        this.btnSaveProfile = new DevExpress.XtraEditors.SimpleButton();
        this.tabTerminalReceipts = new DevExpress.XtraTab.XtraTabPage();
        this.grpScope = new DevExpress.XtraEditors.GroupControl();
        this.lblScopeInfo = new DevExpress.XtraEditors.LabelControl();
        this.lblTerminalDefault = new DevExpress.XtraEditors.LabelControl();
        this.cmbTerminalDefaultReceipt = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblScopeNotice = new DevExpress.XtraEditors.LabelControl();
        this.grpCopies = new DevExpress.XtraEditors.GroupControl();
        this.chkCustomerCopy = new DevExpress.XtraEditors.CheckEdit();
        this.lblCustomerPrinter = new DevExpress.XtraEditors.LabelControl();
        this.cmbCustomerPrinter = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblCustomerCopies = new DevExpress.XtraEditors.LabelControl();
        this.spinCustomerCopies = new DevExpress.XtraEditors.SpinEdit();
        this.chkMerchantCopy = new DevExpress.XtraEditors.CheckEdit();
        this.lblMerchantPrinter = new DevExpress.XtraEditors.LabelControl();
        this.cmbMerchantPrinter = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblMerchantCopies = new DevExpress.XtraEditors.LabelControl();
        this.spinMerchantCopies = new DevExpress.XtraEditors.SpinEdit();
        this.grpLayoutAndBranding = new DevExpress.XtraEditors.GroupControl();
        this.lblTerminalPaperWidth = new DevExpress.XtraEditors.LabelControl();
        this.cmbTerminalPaperWidth = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblMarginMm = new DevExpress.XtraEditors.LabelControl();
        this.spinMarginMm = new DevExpress.XtraEditors.SpinEdit();
        this.lblTerminalColumns = new DevExpress.XtraEditors.LabelControl();
        this.spinTerminalColumns = new DevExpress.XtraEditors.SpinEdit();
        this.chkTerminalCutter = new DevExpress.XtraEditors.CheckEdit();
        this.chkTerminalDrawer = new DevExpress.XtraEditors.CheckEdit();
        this.lblHeaderText = new DevExpress.XtraEditors.LabelControl();
        this.txtHeaderText = new DevExpress.XtraEditors.TextEdit();
        this.lblFooterText = new DevExpress.XtraEditors.LabelControl();
        this.txtFooterText = new DevExpress.XtraEditors.TextEdit();
        this.chkEnableLogo = new DevExpress.XtraEditors.CheckEdit();
        this.txtLogoPath = new DevExpress.XtraEditors.TextEdit();
        this.btnBrowseLogo = new DevExpress.XtraEditors.SimpleButton();
        this.btnPreviewCustomer = new DevExpress.XtraEditors.SimpleButton();
        this.btnPreviewMerchant = new DevExpress.XtraEditors.SimpleButton();
        this.btnSaveTerminalSettings = new DevExpress.XtraEditors.SimpleButton();
        this.panelBottom = new System.Windows.Forms.Panel();
        this.lblStatus = new DevExpress.XtraEditors.LabelControl();
        this.btnPreviewTest = new DevExpress.XtraEditors.SimpleButton();
        this.btnTestPrint = new DevExpress.XtraEditors.SimpleButton();
        this.btnClose = new DevExpress.XtraEditors.SimpleButton();
        ((System.ComponentModel.ISupportInitialize)(this.grpInstalled)).BeginInit();
        this.grpInstalled.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.cmbInstalledPrinters.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.tabControl)).BeginInit();
        this.tabControl.SuspendLayout();
        this.tabProfiles.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.grpProfiles)).BeginInit();
        this.grpProfiles.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.lstProfiles)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.grpProfileDetails)).BeginInit();
        this.grpProfileDetails.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.txtProfileName.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtQueueName.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbRole.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbPaperWidth.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinColumns.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinCopies.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkCutter.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkDrawer.Properties)).BeginInit();
        this.tabTerminalReceipts.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.grpScope)).BeginInit();
        this.grpScope.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.cmbTerminalDefaultReceipt.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.grpCopies)).BeginInit();
        this.grpCopies.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.chkCustomerCopy.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbCustomerPrinter.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinCustomerCopies.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkMerchantCopy.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbMerchantPrinter.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinMerchantCopies.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.grpLayoutAndBranding)).BeginInit();
        this.grpLayoutAndBranding.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.cmbTerminalPaperWidth.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinMarginMm.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinTerminalColumns.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkTerminalCutter.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkTerminalDrawer.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtHeaderText.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtFooterText.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkEnableLogo.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtLogoPath.Properties)).BeginInit();
        this.panelBottom.SuspendLayout();
        this.SuspendLayout();
        // 
        // lblTitle
        // 
        this.lblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblTitle.Appearance.Options.UseFont = true;
        this.lblTitle.Location = new System.Drawing.Point(24, 14);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Size = new System.Drawing.Size(262, 21);
        this.lblTitle.TabIndex = 0;
        this.lblTitle.Text = "Printer & Hardware Management";
        // 
        // lblSubtitle
        // 
        this.lblSubtitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblSubtitle.Appearance.ForeColor = System.Drawing.Color.Gray;
        this.lblSubtitle.Appearance.Options.UseFont = true;
        this.lblSubtitle.Appearance.Options.UseForeColor = true;
        this.lblSubtitle.Location = new System.Drawing.Point(24, 38);
        this.lblSubtitle.Name = "lblSubtitle";
        this.lblSubtitle.Size = new System.Drawing.Size(585, 15);
        this.lblSubtitle.TabIndex = 1;
        this.lblSubtitle.Text = "Configure thermal receipt printers, terminal routing, customer/store copies, paper dimensions, and hardware diagnostics.";
        // 
        // grpInstalled
        // 
        this.grpInstalled.Controls.Add(this.lblCapabilityInfo);
        this.grpInstalled.Controls.Add(this.lblInstalled);
        this.grpInstalled.Controls.Add(this.cmbInstalledPrinters);
        this.grpInstalled.Controls.Add(this.btnRefreshQueues);
        this.grpInstalled.Location = new System.Drawing.Point(24, 60);
        this.grpInstalled.Name = "grpInstalled";
        this.grpInstalled.Size = new System.Drawing.Size(790, 78);
        this.grpInstalled.TabIndex = 2;
        this.grpInstalled.Text = "System Print Queues & Hardware Capability Detection";
        // 
        // lblInstalled
        // 
        this.lblInstalled.Location = new System.Drawing.Point(16, 28);
        this.lblInstalled.Name = "lblInstalled";
        this.lblInstalled.Size = new System.Drawing.Size(123, 13);
        this.lblInstalled.TabIndex = 0;
        this.lblInstalled.Text = "Detected Windows Queue:";
        // 
        // cmbInstalledPrinters
        // 
        this.cmbInstalledPrinters.Location = new System.Drawing.Point(155, 25);
        this.cmbInstalledPrinters.Name = "cmbInstalledPrinters";
        this.cmbInstalledPrinters.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbInstalledPrinters.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbInstalledPrinters.Size = new System.Drawing.Size(480, 20);
        this.cmbInstalledPrinters.TabIndex = 1;
        // 
        // btnRefreshQueues
        // 
        this.btnRefreshQueues.Location = new System.Drawing.Point(650, 23);
        this.btnRefreshQueues.Name = "btnRefreshQueues";
        this.btnRefreshQueues.Size = new System.Drawing.Size(125, 24);
        this.btnRefreshQueues.TabIndex = 2;
        this.btnRefreshQueues.Text = "Refresh Queues";
        // 
        // lblCapabilityInfo
        // 
        this.lblCapabilityInfo.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular);
        this.lblCapabilityInfo.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(64)))), ((int)(((byte)(175)))));
        this.lblCapabilityInfo.Appearance.Options.UseFont = true;
        this.lblCapabilityInfo.Appearance.Options.UseForeColor = true;
        this.lblCapabilityInfo.Location = new System.Drawing.Point(16, 52);
        this.lblCapabilityInfo.Name = "lblCapabilityInfo";
        this.lblCapabilityInfo.Size = new System.Drawing.Size(268, 13);
        this.lblCapabilityInfo.TabIndex = 3;
        this.lblCapabilityInfo.Text = "Hardware Capability: Inspecting selected print queue...";
        // 
        // tabControl
        // 
        this.tabControl.Location = new System.Drawing.Point(24, 146);
        this.tabControl.Name = "tabControl";
        this.tabControl.SelectedTabPage = this.tabProfiles;
        this.tabControl.Size = new System.Drawing.Size(790, 480);
        this.tabControl.TabIndex = 3;
        this.tabControl.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
            this.tabProfiles,
            this.tabTerminalReceipts});
        // 
        // tabProfiles
        // 
        this.tabProfiles.Controls.Add(this.grpProfiles);
        this.tabProfiles.Controls.Add(this.grpProfileDetails);
        this.tabProfiles.Name = "tabProfiles";
        this.tabProfiles.Size = new System.Drawing.Size(788, 452);
        this.tabProfiles.Text = "Printers & Profiles";
        // 
        // grpProfiles
        // 
        this.grpProfiles.Controls.Add(this.lstProfiles);
        this.grpProfiles.Controls.Add(this.btnAddProfile);
        this.grpProfiles.Controls.Add(this.btnDeleteProfile);
        this.grpProfiles.Controls.Add(this.btnSetDefault);
        this.grpProfiles.Controls.Add(this.btnSetTerminalDefault);
        this.grpProfiles.Location = new System.Drawing.Point(10, 10);
        this.grpProfiles.Name = "grpProfiles";
        this.grpProfiles.Size = new System.Drawing.Size(265, 432);
        this.grpProfiles.TabIndex = 0;
        this.grpProfiles.Text = "Configured Profiles";
        // 
        // lstProfiles
        // 
        this.lstProfiles.Location = new System.Drawing.Point(10, 26);
        this.lstProfiles.Name = "lstProfiles";
        this.lstProfiles.Size = new System.Drawing.Size(245, 355);
        this.lstProfiles.TabIndex = 0;
        // 
        // btnAddProfile
        // 
        this.btnAddProfile.Location = new System.Drawing.Point(10, 390);
        this.btnAddProfile.Name = "btnAddProfile";
        this.btnAddProfile.Size = new System.Drawing.Size(55, 26);
        this.btnAddProfile.TabIndex = 1;
        this.btnAddProfile.Text = "+ New";
        // 
        // btnDeleteProfile
        // 
        this.btnDeleteProfile.Location = new System.Drawing.Point(70, 390);
        this.btnDeleteProfile.Name = "btnDeleteProfile";
        this.btnDeleteProfile.Size = new System.Drawing.Size(55, 26);
        this.btnDeleteProfile.TabIndex = 2;
        this.btnDeleteProfile.Text = "Delete";
        // 
        // btnSetDefault
        // 
        this.btnSetDefault.Location = new System.Drawing.Point(130, 390);
        this.btnSetDefault.Name = "btnSetDefault";
        this.btnSetDefault.Size = new System.Drawing.Size(58, 26);
        this.btnSetDefault.TabIndex = 3;
        this.btnSetDefault.Text = "Global";
        // 
        // btnSetTerminalDefault
        // 
        this.btnSetTerminalDefault.Location = new System.Drawing.Point(193, 390);
        this.btnSetTerminalDefault.Name = "btnSetTerminalDefault";
        this.btnSetTerminalDefault.Size = new System.Drawing.Size(62, 26);
        this.btnSetTerminalDefault.TabIndex = 4;
        this.btnSetTerminalDefault.Text = "Terminal";
        // 
        // grpProfileDetails
        // 
        this.grpProfileDetails.Controls.Add(this.lblProfileName);
        this.grpProfileDetails.Controls.Add(this.txtProfileName);
        this.grpProfileDetails.Controls.Add(this.lblQueue);
        this.grpProfileDetails.Controls.Add(this.txtQueueName);
        this.grpProfileDetails.Controls.Add(this.lblRole);
        this.grpProfileDetails.Controls.Add(this.cmbRole);
        this.grpProfileDetails.Controls.Add(this.lblPaperWidth);
        this.grpProfileDetails.Controls.Add(this.cmbPaperWidth);
        this.grpProfileDetails.Controls.Add(this.lblColumns);
        this.grpProfileDetails.Controls.Add(this.spinColumns);
        this.grpProfileDetails.Controls.Add(this.lblCopies);
        this.grpProfileDetails.Controls.Add(this.spinCopies);
        this.grpProfileDetails.Controls.Add(this.chkCutter);
        this.grpProfileDetails.Controls.Add(this.chkDrawer);
        this.grpProfileDetails.Controls.Add(this.lblHardwareNotes);
        this.grpProfileDetails.Controls.Add(this.btnSaveProfile);
        this.grpProfileDetails.Location = new System.Drawing.Point(285, 10);
        this.grpProfileDetails.Name = "grpProfileDetails";
        this.grpProfileDetails.Size = new System.Drawing.Size(490, 432);
        this.grpProfileDetails.TabIndex = 1;
        this.grpProfileDetails.Text = "Profile Configuration";
        // 
        // lblProfileName
        // 
        this.lblProfileName.Location = new System.Drawing.Point(16, 32);
        this.lblProfileName.Name = "lblProfileName";
        this.lblProfileName.Size = new System.Drawing.Size(64, 13);
        this.lblProfileName.TabIndex = 0;
        this.lblProfileName.Text = "Profile Name:";
        // 
        // txtProfileName
        // 
        this.txtProfileName.Location = new System.Drawing.Point(125, 29);
        this.txtProfileName.Name = "txtProfileName";
        this.txtProfileName.Size = new System.Drawing.Size(345, 20);
        this.txtProfileName.TabIndex = 1;
        // 
        // lblQueue
        // 
        this.lblQueue.Location = new System.Drawing.Point(16, 64);
        this.lblQueue.Name = "lblQueue";
        this.lblQueue.Size = new System.Drawing.Size(73, 13);
        this.lblQueue.TabIndex = 2;
        this.lblQueue.Text = "Windows Queue:";
        // 
        // txtQueueName
        // 
        this.txtQueueName.Location = new System.Drawing.Point(125, 61);
        this.txtQueueName.Name = "txtQueueName";
        this.txtQueueName.Size = new System.Drawing.Size(345, 20);
        this.txtQueueName.TabIndex = 3;
        // 
        // lblRole
        // 
        this.lblRole.Location = new System.Drawing.Point(16, 96);
        this.lblRole.Name = "lblRole";
        this.lblRole.Size = new System.Drawing.Size(25, 13);
        this.lblRole.TabIndex = 4;
        this.lblRole.Text = "Role:";
        // 
        // cmbRole
        // 
        this.cmbRole.Location = new System.Drawing.Point(125, 93);
        this.cmbRole.Name = "cmbRole";
        this.cmbRole.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbRole.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbRole.Size = new System.Drawing.Size(345, 20);
        this.cmbRole.TabIndex = 5;
        // 
        // lblPaperWidth
        // 
        this.lblPaperWidth.Location = new System.Drawing.Point(16, 128);
        this.lblPaperWidth.Name = "lblPaperWidth";
        this.lblPaperWidth.Size = new System.Drawing.Size(63, 13);
        this.lblPaperWidth.TabIndex = 6;
        this.lblPaperWidth.Text = "Paper Width:";
        // 
        // cmbPaperWidth
        // 
        this.cmbPaperWidth.Location = new System.Drawing.Point(125, 125);
        this.cmbPaperWidth.Name = "cmbPaperWidth";
        this.cmbPaperWidth.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbPaperWidth.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbPaperWidth.Size = new System.Drawing.Size(345, 20);
        this.cmbPaperWidth.TabIndex = 7;
        // 
        // lblColumns
        // 
        this.lblColumns.Location = new System.Drawing.Point(16, 160);
        this.lblColumns.Name = "lblColumns";
        this.lblColumns.Size = new System.Drawing.Size(69, 13);
        this.lblColumns.TabIndex = 8;
        this.lblColumns.Text = "Columns (CPL):";
        // 
        // spinColumns
        // 
        this.spinColumns.EditValue = new decimal(new int[] { 42, 0, 0, 0 });
        this.spinColumns.Location = new System.Drawing.Point(125, 157);
        this.spinColumns.Name = "spinColumns";
        this.spinColumns.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.spinColumns.Properties.IsFloatValue = false;
        this.spinColumns.Properties.MaxValue = new decimal(new int[] { 120, 0, 0, 0 });
        this.spinColumns.Properties.MinValue = new decimal(new int[] { 28, 0, 0, 0 });
        this.spinColumns.Size = new System.Drawing.Size(100, 20);
        this.spinColumns.TabIndex = 9;
        // 
        // lblCopies
        // 
        this.lblCopies.Location = new System.Drawing.Point(245, 160);
        this.lblCopies.Name = "lblCopies";
        this.lblCopies.Size = new System.Drawing.Size(36, 13);
        this.lblCopies.TabIndex = 10;
        this.lblCopies.Text = "Copies:";
        // 
        // spinCopies
        // 
        this.spinCopies.EditValue = new decimal(new int[] { 1, 0, 0, 0 });
        this.spinCopies.Location = new System.Drawing.Point(300, 157);
        this.spinCopies.Name = "spinCopies";
        this.spinCopies.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.spinCopies.Properties.IsFloatValue = false;
        this.spinCopies.Properties.MaxValue = new decimal(new int[] { 10, 0, 0, 0 });
        this.spinCopies.Properties.MinValue = new decimal(new int[] { 1, 0, 0, 0 });
        this.spinCopies.Size = new System.Drawing.Size(170, 20);
        this.spinCopies.TabIndex = 11;
        // 
        // chkCutter
        // 
        this.chkCutter.EditValue = false;
        this.chkCutter.Location = new System.Drawing.Point(125, 192);
        this.chkCutter.Name = "chkCutter";
        this.chkCutter.Properties.Caption = "Auto Paper Cutter (Unverified)";
        this.chkCutter.Size = new System.Drawing.Size(165, 20);
        this.chkCutter.TabIndex = 12;
        // 
        // chkDrawer
        // 
        this.chkDrawer.EditValue = false;
        this.chkDrawer.Location = new System.Drawing.Point(295, 192);
        this.chkDrawer.Name = "chkDrawer";
        this.chkDrawer.Properties.Caption = "Cash Drawer Kick (Unverified)";
        this.chkDrawer.Size = new System.Drawing.Size(185, 20);
        this.chkDrawer.TabIndex = 13;
        // 
        // lblHardwareNotes
        // 
        this.lblHardwareNotes.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.25F);
        this.lblHardwareNotes.Appearance.ForeColor = System.Drawing.Color.Gray;
        this.lblHardwareNotes.Appearance.Options.UseFont = true;
        this.lblHardwareNotes.Appearance.Options.UseForeColor = true;
        this.lblHardwareNotes.Location = new System.Drawing.Point(16, 225);
        this.lblHardwareNotes.Name = "lblHardwareNotes";
        this.lblHardwareNotes.Size = new System.Drawing.Size(438, 26);
        this.lblHardwareNotes.TabIndex = 14;
        this.lblHardwareNotes.Text = "Note: Hardware capabilities (cutter and cash drawer) remain Unverified until physically confirmed on the device.\r\nVirtual queues (PDF/OneNote) and office document printers have hardware controls disabled.";
        // 
        // btnSaveProfile
        // 
        this.btnSaveProfile.Location = new System.Drawing.Point(125, 385);
        this.btnSaveProfile.Name = "btnSaveProfile";
        this.btnSaveProfile.Size = new System.Drawing.Size(160, 30);
        this.btnSaveProfile.TabIndex = 15;
        this.btnSaveProfile.Text = "Save Profile";
        // 
        // tabTerminalReceipts
        // 
        this.tabTerminalReceipts.Controls.Add(this.btnSaveTerminalSettings);
        this.tabTerminalReceipts.Controls.Add(this.btnPreviewMerchant);
        this.tabTerminalReceipts.Controls.Add(this.btnPreviewCustomer);
        this.tabTerminalReceipts.Controls.Add(this.grpLayoutAndBranding);
        this.tabTerminalReceipts.Controls.Add(this.grpCopies);
        this.tabTerminalReceipts.Controls.Add(this.grpScope);
        this.tabTerminalReceipts.Name = "tabTerminalReceipts";
        this.tabTerminalReceipts.Size = new System.Drawing.Size(788, 452);
        this.tabTerminalReceipts.Text = "Terminal Receipt Setup & Copies";
        // 
        // grpScope
        // 
        this.grpScope.Controls.Add(this.lblScopeNotice);
        this.grpScope.Controls.Add(this.cmbTerminalDefaultReceipt);
        this.grpScope.Controls.Add(this.lblTerminalDefault);
        this.grpScope.Controls.Add(this.lblScopeInfo);
        this.grpScope.Location = new System.Drawing.Point(10, 8);
        this.grpScope.Name = "grpScope";
        this.grpScope.Size = new System.Drawing.Size(768, 72);
        this.grpScope.TabIndex = 0;
        this.grpScope.Text = "Active Terminal Scope & Default Routing";
        // 
        // lblScopeInfo
        // 
        this.lblScopeInfo.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblScopeInfo.Appearance.Options.UseFont = true;
        this.lblScopeInfo.Location = new System.Drawing.Point(14, 25);
        this.lblScopeInfo.Name = "lblScopeInfo";
        this.lblScopeInfo.Size = new System.Drawing.Size(374, 15);
        this.lblScopeInfo.TabIndex = 0;
        this.lblScopeInfo.Text = "Scope: Terminal [Active Workstation] | Branch [Default Store Branch]";
        // 
        // lblTerminalDefault
        // 
        this.lblTerminalDefault.Location = new System.Drawing.Point(14, 47);
        this.lblTerminalDefault.Name = "lblTerminalDefault";
        this.lblTerminalDefault.Size = new System.Drawing.Size(155, 13);
        this.lblTerminalDefault.TabIndex = 1;
        this.lblTerminalDefault.Text = "Terminal Default Receipt Printer:";
        // 
        // cmbTerminalDefaultReceipt
        // 
        this.cmbTerminalDefaultReceipt.Location = new System.Drawing.Point(180, 44);
        this.cmbTerminalDefaultReceipt.Name = "cmbTerminalDefaultReceipt";
        this.cmbTerminalDefaultReceipt.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbTerminalDefaultReceipt.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbTerminalDefaultReceipt.Size = new System.Drawing.Size(260, 20);
        this.cmbTerminalDefaultReceipt.TabIndex = 2;
        // 
        // lblScopeNotice
        // 
        this.lblScopeNotice.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.25F);
        this.lblScopeNotice.Appearance.ForeColor = System.Drawing.Color.Gray;
        this.lblScopeNotice.Appearance.Options.UseFont = true;
        this.lblScopeNotice.Appearance.Options.UseForeColor = true;
        this.lblScopeNotice.Location = new System.Drawing.Point(455, 47);
        this.lblScopeNotice.Name = "lblScopeNotice";
        this.lblScopeNotice.Size = new System.Drawing.Size(298, 13);
        this.lblScopeNotice.TabIndex = 3;
        this.lblScopeNotice.Text = "(CBOS application routing; leaves Windows system default unchanged)";
        // 
        // grpCopies
        // 
        this.grpCopies.Controls.Add(this.spinMerchantCopies);
        this.grpCopies.Controls.Add(this.lblMerchantCopies);
        this.grpCopies.Controls.Add(this.cmbMerchantPrinter);
        this.grpCopies.Controls.Add(this.lblMerchantPrinter);
        this.grpCopies.Controls.Add(this.chkMerchantCopy);
        this.grpCopies.Controls.Add(this.spinCustomerCopies);
        this.grpCopies.Controls.Add(this.lblCustomerCopies);
        this.grpCopies.Controls.Add(this.cmbCustomerPrinter);
        this.grpCopies.Controls.Add(this.lblCustomerPrinter);
        this.grpCopies.Controls.Add(this.chkCustomerCopy);
        this.grpCopies.Location = new System.Drawing.Point(10, 84);
        this.grpCopies.Name = "grpCopies";
        this.grpCopies.Size = new System.Drawing.Size(768, 92);
        this.grpCopies.TabIndex = 1;
        this.grpCopies.Text = "Independent Customer & Store Accounting Copies";
        // 
        // chkCustomerCopy
        // 
        this.chkCustomerCopy.EditValue = true;
        this.chkCustomerCopy.Location = new System.Drawing.Point(14, 28);
        this.chkCustomerCopy.Name = "chkCustomerCopy";
        this.chkCustomerCopy.Properties.Caption = "Print Customer Copy";
        this.chkCustomerCopy.Size = new System.Drawing.Size(160, 20);
        this.chkCustomerCopy.TabIndex = 0;
        // 
        // lblCustomerPrinter
        // 
        this.lblCustomerPrinter.Location = new System.Drawing.Point(185, 31);
        this.lblCustomerPrinter.Name = "lblCustomerPrinter";
        this.lblCustomerPrinter.Size = new System.Drawing.Size(58, 13);
        this.lblCustomerPrinter.TabIndex = 1;
        this.lblCustomerPrinter.Text = "Destination:";
        // 
        // cmbCustomerPrinter
        // 
        this.cmbCustomerPrinter.Location = new System.Drawing.Point(255, 28);
        this.cmbCustomerPrinter.Name = "cmbCustomerPrinter";
        this.cmbCustomerPrinter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbCustomerPrinter.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbCustomerPrinter.Size = new System.Drawing.Size(260, 20);
        this.cmbCustomerPrinter.TabIndex = 2;
        // 
        // lblCustomerCopies
        // 
        this.lblCustomerCopies.Location = new System.Drawing.Point(535, 31);
        this.lblCustomerCopies.Name = "lblCustomerCopies";
        this.lblCustomerCopies.Size = new System.Drawing.Size(36, 13);
        this.lblCustomerCopies.TabIndex = 3;
        this.lblCustomerCopies.Text = "Copies:";
        // 
        // spinCustomerCopies
        // 
        this.spinCustomerCopies.EditValue = new decimal(new int[] { 1, 0, 0, 0 });
        this.spinCustomerCopies.Location = new System.Drawing.Point(585, 28);
        this.spinCustomerCopies.Name = "spinCustomerCopies";
        this.spinCustomerCopies.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.spinCustomerCopies.Properties.IsFloatValue = false;
        this.spinCustomerCopies.Properties.MaxValue = new decimal(new int[] { 10, 0, 0, 0 });
        this.spinCustomerCopies.Properties.MinValue = new decimal(new int[] { 1, 0, 0, 0 });
        this.spinCustomerCopies.Size = new System.Drawing.Size(70, 20);
        this.spinCustomerCopies.TabIndex = 4;
        // 
        // chkMerchantCopy
        // 
        this.chkMerchantCopy.Location = new System.Drawing.Point(14, 58);
        this.chkMerchantCopy.Name = "chkMerchantCopy";
        this.chkMerchantCopy.Properties.Caption = "Print Merchant/Store Copy";
        this.chkMerchantCopy.Size = new System.Drawing.Size(165, 20);
        this.chkMerchantCopy.TabIndex = 5;
        // 
        // lblMerchantPrinter
        // 
        this.lblMerchantPrinter.Location = new System.Drawing.Point(185, 61);
        this.lblMerchantPrinter.Name = "lblMerchantPrinter";
        this.lblMerchantPrinter.Size = new System.Drawing.Size(58, 13);
        this.lblMerchantPrinter.TabIndex = 6;
        this.lblMerchantPrinter.Text = "Destination:";
        // 
        // cmbMerchantPrinter
        // 
        this.cmbMerchantPrinter.Location = new System.Drawing.Point(255, 58);
        this.cmbMerchantPrinter.Name = "cmbMerchantPrinter";
        this.cmbMerchantPrinter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbMerchantPrinter.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbMerchantPrinter.Size = new System.Drawing.Size(260, 20);
        this.cmbMerchantPrinter.TabIndex = 7;
        // 
        // lblMerchantCopies
        // 
        this.lblMerchantCopies.Location = new System.Drawing.Point(535, 61);
        this.lblMerchantCopies.Name = "lblMerchantCopies";
        this.lblMerchantCopies.Size = new System.Drawing.Size(36, 13);
        this.lblMerchantCopies.TabIndex = 8;
        this.lblMerchantCopies.Text = "Copies:";
        // 
        // spinMerchantCopies
        // 
        this.spinMerchantCopies.EditValue = new decimal(new int[] { 1, 0, 0, 0 });
        this.spinMerchantCopies.Location = new System.Drawing.Point(585, 58);
        this.spinMerchantCopies.Name = "spinMerchantCopies";
        this.spinMerchantCopies.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.spinMerchantCopies.Properties.IsFloatValue = false;
        this.spinMerchantCopies.Properties.MaxValue = new decimal(new int[] { 10, 0, 0, 0 });
        this.spinMerchantCopies.Properties.MinValue = new decimal(new int[] { 1, 0, 0, 0 });
        this.spinMerchantCopies.Size = new System.Drawing.Size(70, 20);
        this.spinMerchantCopies.TabIndex = 9;
        // 
        // grpLayoutAndBranding
        // 
        this.grpLayoutAndBranding.Controls.Add(this.btnBrowseLogo);
        this.grpLayoutAndBranding.Controls.Add(this.txtLogoPath);
        this.grpLayoutAndBranding.Controls.Add(this.chkEnableLogo);
        this.grpLayoutAndBranding.Controls.Add(this.txtFooterText);
        this.grpLayoutAndBranding.Controls.Add(this.lblFooterText);
        this.grpLayoutAndBranding.Controls.Add(this.txtHeaderText);
        this.grpLayoutAndBranding.Controls.Add(this.lblHeaderText);
        this.grpLayoutAndBranding.Controls.Add(this.chkTerminalDrawer);
        this.grpLayoutAndBranding.Controls.Add(this.chkTerminalCutter);
        this.grpLayoutAndBranding.Controls.Add(this.spinTerminalColumns);
        this.grpLayoutAndBranding.Controls.Add(this.lblTerminalColumns);
        this.grpLayoutAndBranding.Controls.Add(this.spinMarginMm);
        this.grpLayoutAndBranding.Controls.Add(this.lblMarginMm);
        this.grpLayoutAndBranding.Controls.Add(this.cmbTerminalPaperWidth);
        this.grpLayoutAndBranding.Controls.Add(this.lblTerminalPaperWidth);
        this.grpLayoutAndBranding.Location = new System.Drawing.Point(10, 180);
        this.grpLayoutAndBranding.Name = "grpLayoutAndBranding";
        this.grpLayoutAndBranding.Size = new System.Drawing.Size(768, 220);
        this.grpLayoutAndBranding.TabIndex = 2;
        this.grpLayoutAndBranding.Text = "Receipt Dimensions, Formatting & Branding";
        // 
        // lblTerminalPaperWidth
        // 
        this.lblTerminalPaperWidth.Location = new System.Drawing.Point(14, 30);
        this.lblTerminalPaperWidth.Name = "lblTerminalPaperWidth";
        this.lblTerminalPaperWidth.Size = new System.Drawing.Size(63, 13);
        this.lblTerminalPaperWidth.TabIndex = 0;
        this.lblTerminalPaperWidth.Text = "Paper Width:";
        // 
        // cmbTerminalPaperWidth
        // 
        this.cmbTerminalPaperWidth.Location = new System.Drawing.Point(95, 27);
        this.cmbTerminalPaperWidth.Name = "cmbTerminalPaperWidth";
        this.cmbTerminalPaperWidth.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbTerminalPaperWidth.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbTerminalPaperWidth.Size = new System.Drawing.Size(150, 20);
        this.cmbTerminalPaperWidth.TabIndex = 1;
        // 
        // lblMarginMm
        // 
        this.lblMarginMm.Location = new System.Drawing.Point(265, 30);
        this.lblMarginMm.Name = "lblMarginMm";
        this.lblMarginMm.Size = new System.Drawing.Size(64, 13);
        this.lblMarginMm.TabIndex = 2;
        this.lblMarginMm.Text = "Margins (mm):";
        // 
        // spinMarginMm
        // 
        this.spinMarginMm.EditValue = new decimal(new int[] { 2, 0, 0, 0 });
        this.spinMarginMm.Location = new System.Drawing.Point(345, 27);
        this.spinMarginMm.Name = "spinMarginMm";
        this.spinMarginMm.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.spinMarginMm.Properties.IsFloatValue = false;
        this.spinMarginMm.Properties.MaxValue = new decimal(new int[] { 25, 0, 0, 0 });
        this.spinMarginMm.Properties.MinValue = new decimal(new int[] { 0, 0, 0, 0 });
        this.spinMarginMm.Size = new System.Drawing.Size(60, 20);
        this.spinMarginMm.TabIndex = 3;
        // 
        // lblTerminalColumns
        // 
        this.lblTerminalColumns.Location = new System.Drawing.Point(425, 30);
        this.lblTerminalColumns.Name = "lblTerminalColumns";
        this.lblTerminalColumns.Size = new System.Drawing.Size(69, 13);
        this.lblTerminalColumns.TabIndex = 4;
        this.lblTerminalColumns.Text = "Columns (CPL):";
        // 
        // spinTerminalColumns
        // 
        this.spinTerminalColumns.EditValue = new decimal(new int[] { 42, 0, 0, 0 });
        this.spinTerminalColumns.Location = new System.Drawing.Point(510, 27);
        this.spinTerminalColumns.Name = "spinTerminalColumns";
        this.spinTerminalColumns.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.spinTerminalColumns.Properties.IsFloatValue = false;
        this.spinTerminalColumns.Properties.MaxValue = new decimal(new int[] { 120, 0, 0, 0 });
        this.spinTerminalColumns.Properties.MinValue = new decimal(new int[] { 28, 0, 0, 0 });
        this.spinTerminalColumns.Size = new System.Drawing.Size(70, 20);
        this.spinTerminalColumns.TabIndex = 5;
        // 
        // chkTerminalCutter
        // 
        this.chkTerminalCutter.EditValue = false;
        this.chkTerminalCutter.Location = new System.Drawing.Point(95, 55);
        this.chkTerminalCutter.Name = "chkTerminalCutter";
        this.chkTerminalCutter.Properties.Caption = "Cut receipt paper upon completion (Unverified)";
        this.chkTerminalCutter.Size = new System.Drawing.Size(260, 20);
        this.chkTerminalCutter.TabIndex = 6;
        // 
        // chkTerminalDrawer
        // 
        this.chkTerminalDrawer.EditValue = false;
        this.chkTerminalDrawer.Location = new System.Drawing.Point(370, 55);
        this.chkTerminalDrawer.Name = "chkTerminalDrawer";
        this.chkTerminalDrawer.Properties.Caption = "Open cash drawer on cash sale completion (Unverified)";
        this.chkTerminalDrawer.Size = new System.Drawing.Size(320, 20);
        this.chkTerminalDrawer.TabIndex = 7;
        // 
        // lblHeaderText
        // 
        this.lblHeaderText.Location = new System.Drawing.Point(14, 88);
        this.lblHeaderText.Name = "lblHeaderText";
        this.lblHeaderText.Size = new System.Drawing.Size(65, 13);
        this.lblHeaderText.TabIndex = 8;
        this.lblHeaderText.Text = "Header Text:";
        // 
        // txtHeaderText
        // 
        this.txtHeaderText.Location = new System.Drawing.Point(95, 85);
        this.txtHeaderText.Name = "txtHeaderText";
        this.txtHeaderText.Size = new System.Drawing.Size(655, 20);
        this.txtHeaderText.TabIndex = 9;
        // 
        // lblFooterText
        // 
        this.lblFooterText.Location = new System.Drawing.Point(14, 118);
        this.lblFooterText.Name = "lblFooterText";
        this.lblFooterText.Size = new System.Drawing.Size(61, 13);
        this.lblFooterText.TabIndex = 10;
        this.lblFooterText.Text = "Footer Text:";
        // 
        // txtFooterText
        // 
        this.txtFooterText.Location = new System.Drawing.Point(95, 115);
        this.txtFooterText.Name = "txtFooterText";
        this.txtFooterText.Size = new System.Drawing.Size(655, 20);
        this.txtFooterText.TabIndex = 11;
        // 
        // chkEnableLogo
        // 
        this.chkEnableLogo.Location = new System.Drawing.Point(14, 148);
        this.chkEnableLogo.Name = "chkEnableLogo";
        this.chkEnableLogo.Properties.Caption = "Print Logo:";
        this.chkEnableLogo.Size = new System.Drawing.Size(75, 20);
        this.chkEnableLogo.TabIndex = 12;
        // 
        // txtLogoPath
        // 
        this.txtLogoPath.Location = new System.Drawing.Point(95, 148);
        this.txtLogoPath.Name = "txtLogoPath";
        this.txtLogoPath.Size = new System.Drawing.Size(565, 20);
        this.txtLogoPath.TabIndex = 13;
        // 
        // btnBrowseLogo
        // 
        this.btnBrowseLogo.Location = new System.Drawing.Point(670, 146);
        this.btnBrowseLogo.Name = "btnBrowseLogo";
        this.btnBrowseLogo.Size = new System.Drawing.Size(80, 24);
        this.btnBrowseLogo.TabIndex = 14;
        this.btnBrowseLogo.Text = "Browse...";
        // 
        // btnPreviewCustomer
        // 
        this.btnPreviewCustomer.Location = new System.Drawing.Point(160, 408);
        this.btnPreviewCustomer.Name = "btnPreviewCustomer";
        this.btnPreviewCustomer.Size = new System.Drawing.Size(180, 30);
        this.btnPreviewCustomer.TabIndex = 3;
        this.btnPreviewCustomer.Text = "Preview Customer Receipt";
        // 
        // btnPreviewMerchant
        // 
        this.btnPreviewMerchant.Location = new System.Drawing.Point(355, 408);
        this.btnPreviewMerchant.Name = "btnPreviewMerchant";
        this.btnPreviewMerchant.Size = new System.Drawing.Size(180, 30);
        this.btnPreviewMerchant.TabIndex = 4;
        this.btnPreviewMerchant.Text = "Preview Merchant Receipt";
        // 
        // btnSaveTerminalSettings
        // 
        this.btnSaveTerminalSettings.Location = new System.Drawing.Point(550, 408);
        this.btnSaveTerminalSettings.Name = "btnSaveTerminalSettings";
        this.btnSaveTerminalSettings.Size = new System.Drawing.Size(228, 30);
        this.btnSaveTerminalSettings.TabIndex = 5;
        this.btnSaveTerminalSettings.Text = "Save Terminal Receipt Settings";
        // 
        // panelBottom
        // 
        this.panelBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(247)))));
        this.panelBottom.Controls.Add(this.lblStatus);
        this.panelBottom.Controls.Add(this.btnPreviewTest);
        this.panelBottom.Controls.Add(this.btnTestPrint);
        this.panelBottom.Controls.Add(this.btnClose);
        this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.panelBottom.Location = new System.Drawing.Point(0, 635);
        this.panelBottom.Name = "panelBottom";
        this.panelBottom.Size = new System.Drawing.Size(840, 55);
        this.panelBottom.TabIndex = 4;
        // 
        // lblStatus
        // 
        this.lblStatus.Location = new System.Drawing.Point(24, 20);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(35, 13);
        this.lblStatus.TabIndex = 0;
        this.lblStatus.Text = "Ready.";
        // 
        // btnPreviewTest
        // 
        this.btnPreviewTest.Location = new System.Drawing.Point(430, 12);
        this.btnPreviewTest.Name = "btnPreviewTest";
        this.btnPreviewTest.Size = new System.Drawing.Size(130, 30);
        this.btnPreviewTest.TabIndex = 1;
        this.btnPreviewTest.Text = "Preview Test Slip";
        // 
        // btnTestPrint
        // 
        this.btnTestPrint.Location = new System.Drawing.Point(570, 12);
        this.btnTestPrint.Name = "btnTestPrint";
        this.btnTestPrint.Size = new System.Drawing.Size(150, 30);
        this.btnTestPrint.TabIndex = 2;
        this.btnTestPrint.Text = "Execute Test Print";
        // 
        // btnClose
        // 
        this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.btnClose.Location = new System.Drawing.Point(730, 12);
        this.btnClose.Name = "btnClose";
        this.btnClose.Size = new System.Drawing.Size(90, 30);
        this.btnClose.TabIndex = 3;
        this.btnClose.Text = "Close";
        // 
        // PrinterSettingsForm
        // 
        this.ClientSize = new System.Drawing.Size(840, 690);
        this.Controls.Add(this.panelBottom);
        this.Controls.Add(this.tabControl);
        this.Controls.Add(this.grpInstalled);
        this.Controls.Add(this.lblSubtitle);
        this.Controls.Add(this.lblTitle);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "PrinterSettingsForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Printer & Hardware Settings";
        ((System.ComponentModel.ISupportInitialize)(this.grpInstalled)).EndInit();
        this.grpInstalled.ResumeLayout(false);
        this.grpInstalled.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.cmbInstalledPrinters.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.tabControl)).EndInit();
        this.tabControl.ResumeLayout(false);
        this.tabProfiles.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.grpProfiles)).EndInit();
        this.grpProfiles.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.lstProfiles)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.grpProfileDetails)).EndInit();
        this.grpProfileDetails.ResumeLayout(false);
        this.grpProfileDetails.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.txtProfileName.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtQueueName.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbRole.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbPaperWidth.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinColumns.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinCopies.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkCutter.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkDrawer.Properties)).EndInit();
        this.tabTerminalReceipts.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.grpScope)).EndInit();
        this.grpScope.ResumeLayout(false);
        this.grpScope.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.cmbTerminalDefaultReceipt.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.grpCopies)).EndInit();
        this.grpCopies.ResumeLayout(false);
        this.grpCopies.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.chkCustomerCopy.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbCustomerPrinter.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinCustomerCopies.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkMerchantCopy.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbMerchantPrinter.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinMerchantCopies.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.grpLayoutAndBranding)).EndInit();
        this.grpLayoutAndBranding.ResumeLayout(false);
        this.grpLayoutAndBranding.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.cmbTerminalPaperWidth.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinMarginMm.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinTerminalColumns.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkTerminalCutter.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkTerminalDrawer.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtHeaderText.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtFooterText.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkEnableLogo.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtLogoPath.Properties)).EndInit();
        this.panelBottom.ResumeLayout(false);
        this.panelBottom.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private DevExpress.XtraEditors.LabelControl lblTitle;
    private DevExpress.XtraEditors.LabelControl lblSubtitle;
    private DevExpress.XtraEditors.GroupControl grpInstalled;
    private DevExpress.XtraEditors.LabelControl lblInstalled;
    private DevExpress.XtraEditors.ComboBoxEdit cmbInstalledPrinters;
    private DevExpress.XtraEditors.SimpleButton btnRefreshQueues;
    private DevExpress.XtraEditors.LabelControl lblCapabilityInfo;

    private DevExpress.XtraTab.XtraTabControl tabControl;
    private DevExpress.XtraTab.XtraTabPage tabProfiles;
    private DevExpress.XtraTab.XtraTabPage tabTerminalReceipts;

    private DevExpress.XtraEditors.GroupControl grpProfiles;
    private DevExpress.XtraEditors.ListBoxControl lstProfiles;
    private DevExpress.XtraEditors.SimpleButton btnAddProfile;
    private DevExpress.XtraEditors.SimpleButton btnDeleteProfile;
    private DevExpress.XtraEditors.SimpleButton btnSetDefault;
    private DevExpress.XtraEditors.SimpleButton btnSetTerminalDefault;

    private DevExpress.XtraEditors.GroupControl grpProfileDetails;
    private DevExpress.XtraEditors.LabelControl lblProfileName;
    private DevExpress.XtraEditors.TextEdit txtProfileName;
    private DevExpress.XtraEditors.LabelControl lblQueue;
    private DevExpress.XtraEditors.TextEdit txtQueueName;
    private DevExpress.XtraEditors.LabelControl lblRole;
    private DevExpress.XtraEditors.ComboBoxEdit cmbRole;
    private DevExpress.XtraEditors.LabelControl lblPaperWidth;
    private DevExpress.XtraEditors.ComboBoxEdit cmbPaperWidth;
    private DevExpress.XtraEditors.LabelControl lblColumns;
    private DevExpress.XtraEditors.SpinEdit spinColumns;
    private DevExpress.XtraEditors.LabelControl lblCopies;
    private DevExpress.XtraEditors.SpinEdit spinCopies;
    private DevExpress.XtraEditors.CheckEdit chkCutter;
    private DevExpress.XtraEditors.CheckEdit chkDrawer;
    private DevExpress.XtraEditors.LabelControl lblHardwareNotes;
    private DevExpress.XtraEditors.SimpleButton btnSaveProfile;

    private DevExpress.XtraEditors.GroupControl grpScope;
    private DevExpress.XtraEditors.LabelControl lblScopeInfo;
    private DevExpress.XtraEditors.LabelControl lblTerminalDefault;
    private DevExpress.XtraEditors.ComboBoxEdit cmbTerminalDefaultReceipt;
    private DevExpress.XtraEditors.LabelControl lblScopeNotice;

    private DevExpress.XtraEditors.GroupControl grpCopies;
    private DevExpress.XtraEditors.CheckEdit chkCustomerCopy;
    private DevExpress.XtraEditors.LabelControl lblCustomerPrinter;
    private DevExpress.XtraEditors.ComboBoxEdit cmbCustomerPrinter;
    private DevExpress.XtraEditors.LabelControl lblCustomerCopies;
    private DevExpress.XtraEditors.SpinEdit spinCustomerCopies;
    private DevExpress.XtraEditors.CheckEdit chkMerchantCopy;
    private DevExpress.XtraEditors.LabelControl lblMerchantPrinter;
    private DevExpress.XtraEditors.ComboBoxEdit cmbMerchantPrinter;
    private DevExpress.XtraEditors.LabelControl lblMerchantCopies;
    private DevExpress.XtraEditors.SpinEdit spinMerchantCopies;

    private DevExpress.XtraEditors.GroupControl grpLayoutAndBranding;
    private DevExpress.XtraEditors.LabelControl lblTerminalPaperWidth;
    private DevExpress.XtraEditors.ComboBoxEdit cmbTerminalPaperWidth;
    private DevExpress.XtraEditors.LabelControl lblMarginMm;
    private DevExpress.XtraEditors.SpinEdit spinMarginMm;
    private DevExpress.XtraEditors.LabelControl lblTerminalColumns;
    private DevExpress.XtraEditors.SpinEdit spinTerminalColumns;
    private DevExpress.XtraEditors.CheckEdit chkTerminalCutter;
    private DevExpress.XtraEditors.CheckEdit chkTerminalDrawer;
    private DevExpress.XtraEditors.LabelControl lblHeaderText;
    private DevExpress.XtraEditors.TextEdit txtHeaderText;
    private DevExpress.XtraEditors.LabelControl lblFooterText;
    private DevExpress.XtraEditors.TextEdit txtFooterText;
    private DevExpress.XtraEditors.CheckEdit chkEnableLogo;
    private DevExpress.XtraEditors.TextEdit txtLogoPath;
    private DevExpress.XtraEditors.SimpleButton btnBrowseLogo;

    private DevExpress.XtraEditors.SimpleButton btnPreviewCustomer;
    private DevExpress.XtraEditors.SimpleButton btnPreviewMerchant;
    private DevExpress.XtraEditors.SimpleButton btnSaveTerminalSettings;

    private System.Windows.Forms.Panel panelBottom;
    private DevExpress.XtraEditors.LabelControl lblStatus;
    private DevExpress.XtraEditors.SimpleButton btnPreviewTest;
    private DevExpress.XtraEditors.SimpleButton btnTestPrint;
    private DevExpress.XtraEditors.SimpleButton btnClose;
}
