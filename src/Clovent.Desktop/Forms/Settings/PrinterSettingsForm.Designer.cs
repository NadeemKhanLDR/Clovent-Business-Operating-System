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
        this.grpProfiles = new DevExpress.XtraEditors.GroupControl();
        this.lstProfiles = new DevExpress.XtraEditors.ListBoxControl();
        this.btnAddProfile = new DevExpress.XtraEditors.SimpleButton();
        this.btnDeleteProfile = new DevExpress.XtraEditors.SimpleButton();
        this.btnSetDefault = new DevExpress.XtraEditors.SimpleButton();
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
        this.chkCustomerCopy = new DevExpress.XtraEditors.CheckEdit();
        this.lblCustomerCopies = new DevExpress.XtraEditors.LabelControl();
        this.spinCustomerCopies = new DevExpress.XtraEditors.SpinEdit();
        this.chkMerchantCopy = new DevExpress.XtraEditors.CheckEdit();
        this.lblMerchantCopies = new DevExpress.XtraEditors.LabelControl();
        this.spinMerchantCopies = new DevExpress.XtraEditors.SpinEdit();
        this.lblMerchantTrigger = new DevExpress.XtraEditors.LabelControl();
        this.cmbMerchantTrigger = new DevExpress.XtraEditors.ComboBoxEdit();
        this.chkMerchantSignature = new DevExpress.XtraEditors.CheckEdit();
        this.btnSaveProfile = new DevExpress.XtraEditors.SimpleButton();
        this.btnPreviewTest = new DevExpress.XtraEditors.SimpleButton();
        this.btnTestPrint = new DevExpress.XtraEditors.SimpleButton();
        this.panelBottom = new System.Windows.Forms.Panel();
        this.lblStatus = new DevExpress.XtraEditors.LabelControl();
        this.btnClose = new DevExpress.XtraEditors.SimpleButton();
        ((System.ComponentModel.ISupportInitialize)(this.grpInstalled)).BeginInit();
        this.grpInstalled.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.cmbInstalledPrinters.Properties)).BeginInit();
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
        ((System.ComponentModel.ISupportInitialize)(this.chkCustomerCopy.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinCustomerCopies.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkMerchantCopy.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinMerchantCopies.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbMerchantTrigger.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkMerchantSignature.Properties)).BeginInit();
        this.panelBottom.SuspendLayout();
        this.SuspendLayout();
        // 
        // lblTitle
        // 
        this.lblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 11.5F, System.Drawing.FontStyle.Bold);
        this.lblTitle.Appearance.Options.UseFont = true;
        this.lblTitle.Location = new System.Drawing.Point(18, 10);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Size = new System.Drawing.Size(248, 20);
        this.lblTitle.TabIndex = 0;
        this.lblTitle.Text = "Printer & Hardware Management";
        // 
        // lblSubtitle
        // 
        this.lblSubtitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.lblSubtitle.Appearance.ForeColor = System.Drawing.Color.Gray;
        this.lblSubtitle.Appearance.Options.UseFont = true;
        this.lblSubtitle.Appearance.Options.UseForeColor = true;
        this.lblSubtitle.Location = new System.Drawing.Point(18, 30);
        this.lblSubtitle.Name = "lblSubtitle";
        this.lblSubtitle.Size = new System.Drawing.Size(430, 13);
        this.lblSubtitle.TabIndex = 1;
        this.lblSubtitle.Text = "Configure thermal receipt printers, customer/merchant copies, paper widths, and roles.";
        // 
        // grpInstalled
        // 
        this.grpInstalled.Controls.Add(this.lblInstalled);
        this.grpInstalled.Controls.Add(this.cmbInstalledPrinters);
        this.grpInstalled.Controls.Add(this.btnRefreshQueues);
        this.grpInstalled.Location = new System.Drawing.Point(18, 48);
        this.grpInstalled.Name = "grpInstalled";
        this.grpInstalled.Size = new System.Drawing.Size(748, 54);
        this.grpInstalled.TabIndex = 2;
        this.grpInstalled.Text = "System Print Queues";
        // 
        // lblInstalled
        // 
        this.lblInstalled.Location = new System.Drawing.Point(14, 26);
        this.lblInstalled.Name = "lblInstalled";
        this.lblInstalled.Size = new System.Drawing.Size(123, 13);
        this.lblInstalled.TabIndex = 0;
        this.lblInstalled.Text = "Detected Windows Queue:";
        // 
        // cmbInstalledPrinters
        // 
        this.cmbInstalledPrinters.Location = new System.Drawing.Point(150, 23);
        this.cmbInstalledPrinters.Name = "cmbInstalledPrinters";
        this.cmbInstalledPrinters.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbInstalledPrinters.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbInstalledPrinters.Size = new System.Drawing.Size(470, 20);
        this.cmbInstalledPrinters.TabIndex = 1;
        // 
        // btnRefreshQueues
        // 
        this.btnRefreshQueues.Location = new System.Drawing.Point(630, 21);
        this.btnRefreshQueues.Name = "btnRefreshQueues";
        this.btnRefreshQueues.Size = new System.Drawing.Size(106, 24);
        this.btnRefreshQueues.TabIndex = 2;
        this.btnRefreshQueues.Text = "Refresh Queues";
        // 
        // grpProfiles
        // 
        this.grpProfiles.Controls.Add(this.lstProfiles);
        this.grpProfiles.Controls.Add(this.btnAddProfile);
        this.grpProfiles.Controls.Add(this.btnDeleteProfile);
        this.grpProfiles.Controls.Add(this.btnSetDefault);
        this.grpProfiles.Location = new System.Drawing.Point(18, 108);
        this.grpProfiles.Name = "grpProfiles";
        this.grpProfiles.Size = new System.Drawing.Size(232, 288);
        this.grpProfiles.TabIndex = 3;
        this.grpProfiles.Text = "Configured Profiles";
        // 
        // lstProfiles
        // 
        this.lstProfiles.Location = new System.Drawing.Point(8, 24);
        this.lstProfiles.Name = "lstProfiles";
        this.lstProfiles.Size = new System.Drawing.Size(216, 222);
        this.lstProfiles.TabIndex = 0;
        // 
        // btnAddProfile
        // 
        this.btnAddProfile.Location = new System.Drawing.Point(8, 252);
        this.btnAddProfile.Name = "btnAddProfile";
        this.btnAddProfile.Size = new System.Drawing.Size(58, 26);
        this.btnAddProfile.TabIndex = 1;
        this.btnAddProfile.Text = "+ New";
        // 
        // btnDeleteProfile
        // 
        this.btnDeleteProfile.Location = new System.Drawing.Point(70, 252);
        this.btnDeleteProfile.Name = "btnDeleteProfile";
        this.btnDeleteProfile.Size = new System.Drawing.Size(58, 26);
        this.btnDeleteProfile.TabIndex = 2;
        this.btnDeleteProfile.Text = "Delete";
        // 
        // btnSetDefault
        // 
        this.btnSetDefault.Location = new System.Drawing.Point(132, 252);
        this.btnSetDefault.Name = "btnSetDefault";
        this.btnSetDefault.Size = new System.Drawing.Size(92, 26);
        this.btnSetDefault.TabIndex = 3;
        this.btnSetDefault.Text = "Set Default";
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
        this.grpProfileDetails.Controls.Add(this.chkCustomerCopy);
        this.grpProfileDetails.Controls.Add(this.lblCustomerCopies);
        this.grpProfileDetails.Controls.Add(this.spinCustomerCopies);
        this.grpProfileDetails.Controls.Add(this.chkMerchantCopy);
        this.grpProfileDetails.Controls.Add(this.lblMerchantCopies);
        this.grpProfileDetails.Controls.Add(this.spinMerchantCopies);
        this.grpProfileDetails.Controls.Add(this.lblMerchantTrigger);
        this.grpProfileDetails.Controls.Add(this.cmbMerchantTrigger);
        this.grpProfileDetails.Controls.Add(this.chkMerchantSignature);
        this.grpProfileDetails.Controls.Add(this.btnSaveProfile);
        this.grpProfileDetails.Controls.Add(this.btnPreviewTest);
        this.grpProfileDetails.Controls.Add(this.btnTestPrint);
        this.grpProfileDetails.Location = new System.Drawing.Point(256, 108);
        this.grpProfileDetails.Name = "grpProfileDetails";
        this.grpProfileDetails.Size = new System.Drawing.Size(510, 288);
        this.grpProfileDetails.TabIndex = 4;
        this.grpProfileDetails.Text = "Profile Configuration";
        // 
        // lblProfileName
        // 
        this.lblProfileName.Location = new System.Drawing.Point(12, 27);
        this.lblProfileName.Name = "lblProfileName";
        this.lblProfileName.Size = new System.Drawing.Size(64, 13);
        this.lblProfileName.TabIndex = 0;
        this.lblProfileName.Text = "Profile Name:";
        // 
        // txtProfileName
        // 
        this.txtProfileName.Location = new System.Drawing.Point(90, 24);
        this.txtProfileName.Name = "txtProfileName";
        this.txtProfileName.Size = new System.Drawing.Size(155, 20);
        this.txtProfileName.TabIndex = 1;
        // 
        // lblQueue
        // 
        this.lblQueue.Location = new System.Drawing.Point(255, 27);
        this.lblQueue.Name = "lblQueue";
        this.lblQueue.Size = new System.Drawing.Size(73, 13);
        this.lblQueue.TabIndex = 2;
        this.lblQueue.Text = "Windows Queue:";
        // 
        // txtQueueName
        // 
        this.txtQueueName.Location = new System.Drawing.Point(340, 24);
        this.txtQueueName.Name = "txtQueueName";
        this.txtQueueName.Size = new System.Drawing.Size(158, 20);
        this.txtQueueName.TabIndex = 3;
        // 
        // lblRole
        // 
        this.lblRole.Location = new System.Drawing.Point(12, 53);
        this.lblRole.Name = "lblRole";
        this.lblRole.Size = new System.Drawing.Size(25, 13);
        this.lblRole.TabIndex = 4;
        this.lblRole.Text = "Role:";
        // 
        // cmbRole
        // 
        this.cmbRole.Location = new System.Drawing.Point(90, 50);
        this.cmbRole.Name = "cmbRole";
        this.cmbRole.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbRole.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbRole.Size = new System.Drawing.Size(155, 20);
        this.cmbRole.TabIndex = 5;
        // 
        // lblPaperWidth
        // 
        this.lblPaperWidth.Location = new System.Drawing.Point(255, 53);
        this.lblPaperWidth.Name = "lblPaperWidth";
        this.lblPaperWidth.Size = new System.Drawing.Size(63, 13);
        this.lblPaperWidth.TabIndex = 6;
        this.lblPaperWidth.Text = "Paper Width:";
        // 
        // cmbPaperWidth
        // 
        this.cmbPaperWidth.Location = new System.Drawing.Point(340, 50);
        this.cmbPaperWidth.Name = "cmbPaperWidth";
        this.cmbPaperWidth.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbPaperWidth.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbPaperWidth.Size = new System.Drawing.Size(158, 20);
        this.cmbPaperWidth.TabIndex = 7;
        // 
        // lblColumns
        // 
        this.lblColumns.Location = new System.Drawing.Point(12, 79);
        this.lblColumns.Name = "lblColumns";
        this.lblColumns.Size = new System.Drawing.Size(69, 13);
        this.lblColumns.TabIndex = 8;
        this.lblColumns.Text = "Columns (CPL):";
        // 
        // spinColumns
        // 
        this.spinColumns.EditValue = new decimal(new int[] {
            42,
            0,
            0,
            0});
        this.spinColumns.Location = new System.Drawing.Point(90, 76);
        this.spinColumns.Name = "spinColumns";
        this.spinColumns.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.spinColumns.Properties.IsFloatValue = false;
        this.spinColumns.Properties.MaskSettings.Set("mask", "N00");
        this.spinColumns.Properties.MaxValue = new decimal(new int[] {
            120,
            0,
            0,
            0});
        this.spinColumns.Properties.MinValue = new decimal(new int[] {
            28,
            0,
            0,
            0});
        this.spinColumns.Size = new System.Drawing.Size(60, 20);
        this.spinColumns.TabIndex = 9;
        // 
        // lblCopies
        // 
        this.lblCopies.Location = new System.Drawing.Point(400, 79);
        this.lblCopies.Name = "lblCopies";
        this.lblCopies.Size = new System.Drawing.Size(36, 13);
        this.lblCopies.TabIndex = 10;
        this.lblCopies.Text = "Copies:";
        this.lblCopies.Visible = false;
        // 
        // spinCopies
        // 
        this.spinCopies.EditValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
        this.spinCopies.Location = new System.Drawing.Point(445, 76);
        this.spinCopies.Name = "spinCopies";
        this.spinCopies.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.spinCopies.Properties.IsFloatValue = false;
        this.spinCopies.Properties.MaxValue = new decimal(new int[] {
            10,
            0,
            0,
            0});
        this.spinCopies.Properties.MinValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
        this.spinCopies.Size = new System.Drawing.Size(50, 20);
        this.spinCopies.TabIndex = 11;
        this.spinCopies.Visible = false;
        // 
        // chkCutter
        // 
        this.chkCutter.EditValue = true;
        this.chkCutter.Location = new System.Drawing.Point(165, 76);
        this.chkCutter.Name = "chkCutter";
        this.chkCutter.Properties.Caption = "Auto Paper Cutter";
        this.chkCutter.Size = new System.Drawing.Size(120, 20);
        this.chkCutter.TabIndex = 12;
        // 
        // chkDrawer
        // 
        this.chkDrawer.EditValue = true;
        this.chkDrawer.Location = new System.Drawing.Point(295, 76);
        this.chkDrawer.Name = "chkDrawer";
        this.chkDrawer.Properties.Caption = "Cash Drawer Kick";
        this.chkDrawer.Size = new System.Drawing.Size(120, 20);
        this.chkDrawer.TabIndex = 13;
        // 
        // chkCustomerCopy
        // 
        this.chkCustomerCopy.EditValue = true;
        this.chkCustomerCopy.Location = new System.Drawing.Point(12, 104);
        this.chkCustomerCopy.Name = "chkCustomerCopy";
        this.chkCustomerCopy.Properties.Caption = "Customer Copy";
        this.chkCustomerCopy.Size = new System.Drawing.Size(110, 20);
        this.chkCustomerCopy.TabIndex = 14;
        // 
        // lblCustomerCopies
        // 
        this.lblCustomerCopies.Location = new System.Drawing.Point(128, 107);
        this.lblCustomerCopies.Name = "lblCustomerCopies";
        this.lblCustomerCopies.Size = new System.Drawing.Size(36, 13);
        this.lblCustomerCopies.TabIndex = 15;
        this.lblCustomerCopies.Text = "Copies:";
        // 
        // spinCustomerCopies
        // 
        this.spinCustomerCopies.EditValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
        this.spinCustomerCopies.Location = new System.Drawing.Point(170, 104);
        this.spinCustomerCopies.Name = "spinCustomerCopies";
        this.spinCustomerCopies.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.spinCustomerCopies.Properties.IsFloatValue = false;
        this.spinCustomerCopies.Properties.MaxValue = new decimal(new int[] {
            10,
            0,
            0,
            0});
        this.spinCustomerCopies.Properties.MinValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
        this.spinCustomerCopies.Size = new System.Drawing.Size(50, 20);
        this.spinCustomerCopies.TabIndex = 16;
        // 
        // chkMerchantCopy
        // 
        this.chkMerchantCopy.EditValue = false;
        this.chkMerchantCopy.Location = new System.Drawing.Point(240, 104);
        this.chkMerchantCopy.Name = "chkMerchantCopy";
        this.chkMerchantCopy.Properties.Caption = "Merchant Copy";
        this.chkMerchantCopy.Size = new System.Drawing.Size(110, 20);
        this.chkMerchantCopy.TabIndex = 17;
        // 
        // lblMerchantCopies
        // 
        this.lblMerchantCopies.Location = new System.Drawing.Point(356, 107);
        this.lblMerchantCopies.Name = "lblMerchantCopies";
        this.lblMerchantCopies.Size = new System.Drawing.Size(36, 13);
        this.lblMerchantCopies.TabIndex = 18;
        this.lblMerchantCopies.Text = "Copies:";
        // 
        // spinMerchantCopies
        // 
        this.spinMerchantCopies.EditValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
        this.spinMerchantCopies.Location = new System.Drawing.Point(398, 104);
        this.spinMerchantCopies.Name = "spinMerchantCopies";
        this.spinMerchantCopies.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.spinMerchantCopies.Properties.IsFloatValue = false;
        this.spinMerchantCopies.Properties.MaxValue = new decimal(new int[] {
            10,
            0,
            0,
            0});
        this.spinMerchantCopies.Properties.MinValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
        this.spinMerchantCopies.Size = new System.Drawing.Size(50, 20);
        this.spinMerchantCopies.TabIndex = 19;
        // 
        // lblMerchantTrigger
        // 
        this.lblMerchantTrigger.Location = new System.Drawing.Point(12, 133);
        this.lblMerchantTrigger.Name = "lblMerchantTrigger";
        this.lblMerchantTrigger.Size = new System.Drawing.Size(80, 13);
        this.lblMerchantTrigger.TabIndex = 20;
        this.lblMerchantTrigger.Text = "Merchant Policy:";
        // 
        // cmbMerchantTrigger
        // 
        this.cmbMerchantTrigger.Location = new System.Drawing.Point(98, 130);
        this.cmbMerchantTrigger.Name = "cmbMerchantTrigger";
        this.cmbMerchantTrigger.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbMerchantTrigger.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbMerchantTrigger.Size = new System.Drawing.Size(150, 20);
        this.cmbMerchantTrigger.TabIndex = 21;
        // 
        // chkMerchantSignature
        // 
        this.chkMerchantSignature.EditValue = true;
        this.chkMerchantSignature.Location = new System.Drawing.Point(260, 130);
        this.chkMerchantSignature.Name = "chkMerchantSignature";
        this.chkMerchantSignature.Properties.Caption = "Customer Signature Line";
        this.chkMerchantSignature.Size = new System.Drawing.Size(160, 20);
        this.chkMerchantSignature.TabIndex = 22;
        // 
        // btnSaveProfile
        // 
        this.btnSaveProfile.Location = new System.Drawing.Point(12, 162);
        this.btnSaveProfile.Name = "btnSaveProfile";
        this.btnSaveProfile.Size = new System.Drawing.Size(115, 30);
        this.btnSaveProfile.TabIndex = 23;
        this.btnSaveProfile.Text = "Save Profile";
        // 
        // btnPreviewTest
        // 
        this.btnPreviewTest.Location = new System.Drawing.Point(135, 162);
        this.btnPreviewTest.Name = "btnPreviewTest";
        this.btnPreviewTest.Size = new System.Drawing.Size(120, 30);
        this.btnPreviewTest.TabIndex = 24;
        this.btnPreviewTest.Text = "Preview Test Slip";
        // 
        // btnTestPrint
        // 
        this.btnTestPrint.Location = new System.Drawing.Point(263, 162);
        this.btnTestPrint.Name = "btnTestPrint";
        this.btnTestPrint.Size = new System.Drawing.Size(130, 30);
        this.btnTestPrint.TabIndex = 25;
        this.btnTestPrint.Text = "Execute Test Print";
        // 
        // panelBottom
        // 
        this.panelBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(247)))));
        this.panelBottom.Controls.Add(this.lblStatus);
        this.panelBottom.Controls.Add(this.btnClose);
        this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.panelBottom.Location = new System.Drawing.Point(0, 404);
        this.panelBottom.Name = "panelBottom";
        this.panelBottom.Size = new System.Drawing.Size(784, 32);
        this.panelBottom.TabIndex = 5;
        // 
        // lblStatus
        // 
        this.lblStatus.Location = new System.Drawing.Point(18, 9);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(35, 13);
        this.lblStatus.TabIndex = 0;
        this.lblStatus.Text = "Ready.";
        // 
        // btnClose
        // 
        this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.btnClose.Location = new System.Drawing.Point(700, 4);
        this.btnClose.Name = "btnClose";
        this.btnClose.Size = new System.Drawing.Size(74, 24);
        this.btnClose.TabIndex = 3;
        this.btnClose.Text = "Close";
        this.btnClose.Visible = false;
        // 
        // PrinterSettingsForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.AutoScroll = true;
        this.Size = new System.Drawing.Size(784, 436);
        this.Controls.Add(this.panelBottom);
        this.Controls.Add(this.grpProfileDetails);
        this.Controls.Add(this.grpProfiles);
        this.Controls.Add(this.grpInstalled);
        this.Controls.Add(this.lblSubtitle);
        this.Controls.Add(this.lblTitle);
        this.Name = "PrinterSettingsForm";
        this.Text = "Printer & Hardware Settings";
        ((System.ComponentModel.ISupportInitialize)(this.grpInstalled)).EndInit();
        this.grpInstalled.ResumeLayout(false);
        this.grpInstalled.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.cmbInstalledPrinters.Properties)).EndInit();
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
        ((System.ComponentModel.ISupportInitialize)(this.chkCustomerCopy.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinCustomerCopies.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkMerchantCopy.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.spinMerchantCopies.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbMerchantTrigger.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkMerchantSignature.Properties)).EndInit();
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
    private DevExpress.XtraEditors.GroupControl grpProfiles;
    private DevExpress.XtraEditors.ListBoxControl lstProfiles;
    private DevExpress.XtraEditors.SimpleButton btnAddProfile;
    private DevExpress.XtraEditors.SimpleButton btnDeleteProfile;
    private DevExpress.XtraEditors.SimpleButton btnSetDefault;
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
    private DevExpress.XtraEditors.CheckEdit chkCustomerCopy;
    private DevExpress.XtraEditors.LabelControl lblCustomerCopies;
    private DevExpress.XtraEditors.SpinEdit spinCustomerCopies;
    private DevExpress.XtraEditors.CheckEdit chkMerchantCopy;
    private DevExpress.XtraEditors.LabelControl lblMerchantCopies;
    private DevExpress.XtraEditors.SpinEdit spinMerchantCopies;
    private DevExpress.XtraEditors.LabelControl lblMerchantTrigger;
    private DevExpress.XtraEditors.ComboBoxEdit cmbMerchantTrigger;
    private DevExpress.XtraEditors.CheckEdit chkMerchantSignature;
    private DevExpress.XtraEditors.SimpleButton btnSaveProfile;
    private DevExpress.XtraEditors.SimpleButton btnPreviewTest;
    private DevExpress.XtraEditors.SimpleButton btnTestPrint;
    private System.Windows.Forms.Panel panelBottom;
    private DevExpress.XtraEditors.LabelControl lblStatus;
    private DevExpress.XtraEditors.SimpleButton btnClose;
}
