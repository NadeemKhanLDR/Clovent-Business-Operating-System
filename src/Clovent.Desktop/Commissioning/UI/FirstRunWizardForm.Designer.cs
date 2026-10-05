namespace Clovent.Desktop.Commissioning.UI;

partial class FirstRunWizardForm
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

    private void InitializeComponent()
    {
        this.panelHeader = new System.Windows.Forms.Panel();
        this.lblWizardTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblWizardSubtitle = new DevExpress.XtraEditors.LabelControl();
        this.panelSidebar = new System.Windows.Forms.Panel();
        this.lblSidebarHeader = new DevExpress.XtraEditors.LabelControl();
        this.lblStep1 = new DevExpress.XtraEditors.LabelControl();
        this.lblStep2 = new DevExpress.XtraEditors.LabelControl();
        this.lblStep3 = new DevExpress.XtraEditors.LabelControl();
        this.lblStep4 = new DevExpress.XtraEditors.LabelControl();
        this.lblStep5 = new DevExpress.XtraEditors.LabelControl();
        this.lblStep6 = new DevExpress.XtraEditors.LabelControl();
        this.lblStep7 = new DevExpress.XtraEditors.LabelControl();
        this.lblStep8 = new DevExpress.XtraEditors.LabelControl();
        this.panelBottom = new System.Windows.Forms.Panel();
        this.lblStepIndicator = new DevExpress.XtraEditors.LabelControl();
        this.lblFooterStatus = new DevExpress.XtraEditors.LabelControl();
        this.btnCancel = new DevExpress.XtraEditors.SimpleButton();
        this.btnBack = new DevExpress.XtraEditors.SimpleButton();
        this.btnNext = new DevExpress.XtraEditors.SimpleButton();
        this.btnFinish = new DevExpress.XtraEditors.SimpleButton();
        this.panelContainer = new System.Windows.Forms.Panel();
        this.panelStep1 = new System.Windows.Forms.Panel();
        this.lblStep1Title = new DevExpress.XtraEditors.LabelControl();
        this.lblStep1Desc = new DevExpress.XtraEditors.LabelControl();
        this.grpPrerequisites = new DevExpress.XtraEditors.GroupControl();
        this.lblPrereqSql = new DevExpress.XtraEditors.LabelControl();
        this.lblPrereqRuntime = new DevExpress.XtraEditors.LabelControl();
        this.lblPrereqDisplay = new DevExpress.XtraEditors.LabelControl();
        this.lblPrereqAdmin = new DevExpress.XtraEditors.LabelControl();
        this.grpSystemDetection = new DevExpress.XtraEditors.GroupControl();
        this.lblDetectedOs = new DevExpress.XtraEditors.LabelControl();
        this.lblDetectedRuntime = new DevExpress.XtraEditors.LabelControl();
        this.lblDetectedDpi = new DevExpress.XtraEditors.LabelControl();
        this.lblDetectedElevation = new DevExpress.XtraEditors.LabelControl();
        this.panelStep2 = new System.Windows.Forms.Panel();
        this.lblStep2Title = new DevExpress.XtraEditors.LabelControl();
        this.lblStep2Desc = new DevExpress.XtraEditors.LabelControl();
        this.lblServer = new DevExpress.XtraEditors.LabelControl();
        this.txtServer = new DevExpress.XtraEditors.TextEdit();
        this.lblDatabase = new DevExpress.XtraEditors.LabelControl();
        this.txtDatabase = new DevExpress.XtraEditors.TextEdit();
        this.lblAuth = new DevExpress.XtraEditors.LabelControl();
        this.cmbAuth = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblUsername = new DevExpress.XtraEditors.LabelControl();
        this.txtUsername = new DevExpress.XtraEditors.TextEdit();
        this.lblPassword = new DevExpress.XtraEditors.LabelControl();
        this.txtPassword = new DevExpress.XtraEditors.TextEdit();
        this.btnTestConnection = new DevExpress.XtraEditors.SimpleButton();
        this.lblConnectionStatus = new DevExpress.XtraEditors.LabelControl();
        this.panelStep3 = new System.Windows.Forms.Panel();
        this.lblStep3Title = new DevExpress.XtraEditors.LabelControl();
        this.lblStep3Desc = new DevExpress.XtraEditors.LabelControl();
        this.chkCreateDbIfMissing = new DevExpress.XtraEditors.CheckEdit();
        this.chkApplyMigrations = new DevExpress.XtraEditors.CheckEdit();
        this.btnApplyMigrations = new DevExpress.XtraEditors.SimpleButton();
        this.progressMigrations = new DevExpress.XtraEditors.ProgressBarControl();
        this.lblMigrationStatus = new DevExpress.XtraEditors.LabelControl();
        this.memoMigrationLog = new DevExpress.XtraEditors.MemoEdit();
        this.panelStep4 = new System.Windows.Forms.Panel();
        this.lblStep4Title = new DevExpress.XtraEditors.LabelControl();
        this.lblStep4Desc = new DevExpress.XtraEditors.LabelControl();
        this.lblOrgName = new DevExpress.XtraEditors.LabelControl();
        this.txtOrgName = new DevExpress.XtraEditors.TextEdit();
        this.lblTaxId = new DevExpress.XtraEditors.LabelControl();
        this.txtTaxId = new DevExpress.XtraEditors.TextEdit();
        this.lblCompanyName = new DevExpress.XtraEditors.LabelControl();
        this.txtCompanyName = new DevExpress.XtraEditors.TextEdit();
        this.lblBranchName = new DevExpress.XtraEditors.LabelControl();
        this.txtBranchName = new DevExpress.XtraEditors.TextEdit();
        this.panelStep5 = new System.Windows.Forms.Panel();
        this.lblStep5Title = new DevExpress.XtraEditors.LabelControl();
        this.lblStep5Desc = new DevExpress.XtraEditors.LabelControl();
        this.lblAdminUsername = new DevExpress.XtraEditors.LabelControl();
        this.txtAdminUsername = new DevExpress.XtraEditors.TextEdit();
        this.lblAdminFullName = new DevExpress.XtraEditors.LabelControl();
        this.txtAdminFullName = new DevExpress.XtraEditors.TextEdit();
        this.lblAdminEmail = new DevExpress.XtraEditors.LabelControl();
        this.txtAdminEmail = new DevExpress.XtraEditors.TextEdit();
        this.lblAdminPassword = new DevExpress.XtraEditors.LabelControl();
        this.txtAdminPassword = new DevExpress.XtraEditors.TextEdit();
        this.lblAdminConfirmPassword = new DevExpress.XtraEditors.LabelControl();
        this.txtAdminConfirmPassword = new DevExpress.XtraEditors.TextEdit();
        this.lblPasswordStrength = new DevExpress.XtraEditors.LabelControl();
        this.progressPasswordStrength = new DevExpress.XtraEditors.ProgressBarControl();
        this.lblPasswordPolicy = new DevExpress.XtraEditors.LabelControl();
        this.panelStep6 = new System.Windows.Forms.Panel();
        this.lblStep6Title = new DevExpress.XtraEditors.LabelControl();
        this.lblStep6Desc = new DevExpress.XtraEditors.LabelControl();
        this.lblTimeZone = new DevExpress.XtraEditors.LabelControl();
        this.cmbTimeZone = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblDateFormat = new DevExpress.XtraEditors.LabelControl();
        this.cmbDateFormat = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblTimeFormat = new DevExpress.XtraEditors.LabelControl();
        this.cmbTimeFormat = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblCurrency = new DevExpress.XtraEditors.LabelControl();
        this.cmbCurrency = new DevExpress.XtraEditors.ComboBoxEdit();
        this.lblTerminalName = new DevExpress.XtraEditors.LabelControl();
        this.txtTerminalName = new DevExpress.XtraEditors.TextEdit();
        this.lblSamplePreview = new DevExpress.XtraEditors.LabelControl();
        this.panelStep7 = new System.Windows.Forms.Panel();
        this.lblStep7Title = new DevExpress.XtraEditors.LabelControl();
        this.lblStep7Desc = new DevExpress.XtraEditors.LabelControl();
        this.lblHardwareId = new DevExpress.XtraEditors.LabelControl();
        this.txtHardwareId = new DevExpress.XtraEditors.TextEdit();
        this.btnCopyHardwareId = new DevExpress.XtraEditors.SimpleButton();
        this.btnImportLicense = new DevExpress.XtraEditors.SimpleButton();
        this.lblLicenseStatus = new DevExpress.XtraEditors.LabelControl();
        this.lblLicensedTo = new DevExpress.XtraEditors.LabelControl();
        this.chkEvaluationMode = new DevExpress.XtraEditors.CheckEdit();
        this.panelStep8 = new System.Windows.Forms.Panel();
        this.lblStep8Title = new DevExpress.XtraEditors.LabelControl();
        this.lblStep8Desc = new DevExpress.XtraEditors.LabelControl();
        this.memoSummary = new DevExpress.XtraEditors.MemoEdit();
        this.lblFinishNotice = new DevExpress.XtraEditors.LabelControl();
        this.panelHeader.SuspendLayout();
        this.panelSidebar.SuspendLayout();
        this.panelBottom.SuspendLayout();
        this.panelContainer.SuspendLayout();
        this.panelStep1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.grpPrerequisites)).BeginInit();
        this.grpPrerequisites.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.grpSystemDetection)).BeginInit();
        this.grpSystemDetection.SuspendLayout();
        this.panelStep2.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.txtServer.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtDatabase.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbAuth.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtUsername.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtPassword.Properties)).BeginInit();
        this.panelStep3.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.chkCreateDbIfMissing.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkApplyMigrations.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.progressMigrations.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.memoMigrationLog.Properties)).BeginInit();
        this.panelStep4.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.txtOrgName.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtTaxId.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtCompanyName.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtBranchName.Properties)).BeginInit();
        this.panelStep5.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.txtAdminUsername.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtAdminFullName.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtAdminEmail.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtAdminPassword.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtAdminConfirmPassword.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.progressPasswordStrength.Properties)).BeginInit();
        this.panelStep6.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.cmbTimeZone.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbDateFormat.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbTimeFormat.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbCurrency.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtTerminalName.Properties)).BeginInit();
        this.panelStep7.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.txtHardwareId.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkEvaluationMode.Properties)).BeginInit();
        this.panelStep8.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.memoSummary.Properties)).BeginInit();
        this.SuspendLayout();
        // 
        // panelHeader
        // 
        this.panelHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
        this.panelHeader.Controls.Add(this.lblWizardTitle);
        this.panelHeader.Controls.Add(this.lblWizardSubtitle);
        this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
        this.panelHeader.Location = new System.Drawing.Point(0, 0);
        this.panelHeader.Name = "panelHeader";
        this.panelHeader.Padding = new System.Windows.Forms.Padding(24, 14, 24, 10);
        this.panelHeader.Size = new System.Drawing.Size(934, 70);
        this.panelHeader.TabIndex = 0;
        // 
        // lblWizardTitle
        // 
        this.lblWizardTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12.5F, System.Drawing.FontStyle.Bold);
        this.lblWizardTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
        this.lblWizardTitle.Appearance.Options.UseFont = true;
        this.lblWizardTitle.Appearance.Options.UseForeColor = true;
        this.lblWizardTitle.Location = new System.Drawing.Point(24, 12);
        this.lblWizardTitle.Name = "lblWizardTitle";
        this.lblWizardTitle.Size = new System.Drawing.Size(325, 23);
        this.lblWizardTitle.TabIndex = 0;
        this.lblWizardTitle.Text = "First-Run Setup && Commissioning Wizard";
        // 
        // lblWizardSubtitle
        // 
        this.lblWizardSubtitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblWizardSubtitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblWizardSubtitle.Appearance.Options.UseFont = true;
        this.lblWizardSubtitle.Appearance.Options.UseForeColor = true;
        this.lblWizardSubtitle.Location = new System.Drawing.Point(24, 38);
        this.lblWizardSubtitle.Name = "lblWizardSubtitle";
        this.lblWizardSubtitle.Size = new System.Drawing.Size(222, 15);
        this.lblWizardSubtitle.TabIndex = 1;
        this.lblWizardSubtitle.Text = "Step 1 of 8: Welcome && System Overview";
        this.lblWizardSubtitle.UseMnemonic = false;
        // 
        // panelSidebar
        // 
        this.panelSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
        this.panelSidebar.Controls.Add(this.lblSidebarHeader);
        this.panelSidebar.Controls.Add(this.lblStep1);
        this.panelSidebar.Controls.Add(this.lblStep2);
        this.panelSidebar.Controls.Add(this.lblStep3);
        this.panelSidebar.Controls.Add(this.lblStep4);
        this.panelSidebar.Controls.Add(this.lblStep5);
        this.panelSidebar.Controls.Add(this.lblStep6);
        this.panelSidebar.Controls.Add(this.lblStep7);
        this.panelSidebar.Controls.Add(this.lblStep8);
        this.panelSidebar.Dock = System.Windows.Forms.DockStyle.Left;
        this.panelSidebar.Location = new System.Drawing.Point(0, 70);
        this.panelSidebar.Name = "panelSidebar";
        this.panelSidebar.Padding = new System.Windows.Forms.Padding(16, 18, 16, 18);
        this.panelSidebar.Size = new System.Drawing.Size(240, 526);
        this.panelSidebar.TabIndex = 1;
        // 
        // lblSidebarHeader
        // 
        this.lblSidebarHeader.Appearance.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
        this.lblSidebarHeader.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblSidebarHeader.Appearance.Options.UseFont = true;
        this.lblSidebarHeader.Appearance.Options.UseForeColor = true;
        this.lblSidebarHeader.Location = new System.Drawing.Point(16, 16);
        this.lblSidebarHeader.Name = "lblSidebarHeader";
        this.lblSidebarHeader.Size = new System.Drawing.Size(76, 13);
        this.lblSidebarHeader.TabIndex = 0;
        this.lblSidebarHeader.Text = "SETUP STEPS";
        // 
        // lblStep1
        // 
        this.lblStep1.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblStep1.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
        this.lblStep1.Appearance.Options.UseFont = true;
        this.lblStep1.Appearance.Options.UseForeColor = true;
        this.lblStep1.Location = new System.Drawing.Point(16, 44);
        this.lblStep1.Name = "lblStep1";
        this.lblStep1.Size = new System.Drawing.Size(126, 15);
        this.lblStep1.TabIndex = 1;
        this.lblStep1.Text = "1. Welcome && Overview";
        // 
        // lblStep2
        // 
        this.lblStep2.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep2.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblStep2.Appearance.Options.UseFont = true;
        this.lblStep2.Appearance.Options.UseForeColor = true;
        this.lblStep2.Location = new System.Drawing.Point(16, 76);
        this.lblStep2.Name = "lblStep2";
        this.lblStep2.Size = new System.Drawing.Size(126, 15);
        this.lblStep2.TabIndex = 2;
        this.lblStep2.Text = "2. Database Connection";
        // 
        // lblStep3
        // 
        this.lblStep3.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep3.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblStep3.Appearance.Options.UseFont = true;
        this.lblStep3.Appearance.Options.UseForeColor = true;
        this.lblStep3.Location = new System.Drawing.Point(16, 108);
        this.lblStep3.Name = "lblStep3";
        this.lblStep3.Size = new System.Drawing.Size(130, 15);
        this.lblStep3.TabIndex = 3;
        this.lblStep3.Text = "3. Schema Initialization";
        // 
        // lblStep4
        // 
        this.lblStep4.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep4.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblStep4.Appearance.Options.UseFont = true;
        this.lblStep4.Appearance.Options.UseForeColor = true;
        this.lblStep4.Location = new System.Drawing.Point(16, 140);
        this.lblStep4.Name = "lblStep4";
        this.lblStep4.Size = new System.Drawing.Size(123, 15);
        this.lblStep4.TabIndex = 4;
        this.lblStep4.Text = "4. Enterprise Hierarchy";
        // 
        // lblStep5
        // 
        this.lblStep5.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep5.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblStep5.Appearance.Options.UseFont = true;
        this.lblStep5.Appearance.Options.UseForeColor = true;
        this.lblStep5.Location = new System.Drawing.Point(16, 172);
        this.lblStep5.Name = "lblStep5";
        this.lblStep5.Size = new System.Drawing.Size(138, 15);
        this.lblStep5.TabIndex = 5;
        this.lblStep5.Text = "5. Administrator Account";
        // 
        // lblStep6
        // 
        this.lblStep6.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep6.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblStep6.Appearance.Options.UseFont = true;
        this.lblStep6.Appearance.Options.UseForeColor = true;
        this.lblStep6.Location = new System.Drawing.Point(16, 204);
        this.lblStep6.Name = "lblStep6";
        this.lblStep6.Size = new System.Drawing.Size(110, 15);
        this.lblStep6.TabIndex = 6;
        this.lblStep6.Text = "6. Regional Settings";
        // 
        // lblStep7
        // 
        this.lblStep7.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep7.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblStep7.Appearance.Options.UseFont = true;
        this.lblStep7.Appearance.Options.UseForeColor = true;
        this.lblStep7.Location = new System.Drawing.Point(16, 236);
        this.lblStep7.Name = "lblStep7";
        this.lblStep7.Size = new System.Drawing.Size(131, 15);
        this.lblStep7.TabIndex = 7;
        this.lblStep7.Text = "7. Licensing && Activation";
        // 
        // lblStep8
        // 
        this.lblStep8.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep8.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblStep8.Appearance.Options.UseFont = true;
        this.lblStep8.Appearance.Options.UseForeColor = true;
        this.lblStep8.Location = new System.Drawing.Point(16, 268);
        this.lblStep8.Name = "lblStep8";
        this.lblStep8.Size = new System.Drawing.Size(117, 15);
        this.lblStep8.TabIndex = 8;
        this.lblStep8.Text = "8. Review && Complete";
        // 
        // panelBottom
        // 
        this.panelBottom.Controls.Add(this.lblStepIndicator);
        this.panelBottom.Controls.Add(this.lblFooterStatus);
        this.panelBottom.Controls.Add(this.btnCancel);
        this.panelBottom.Controls.Add(this.btnBack);
        this.panelBottom.Controls.Add(this.btnNext);
        this.panelBottom.Controls.Add(this.btnFinish);
        this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.panelBottom.Location = new System.Drawing.Point(0, 596);
        this.panelBottom.Name = "panelBottom";
        this.panelBottom.Padding = new System.Windows.Forms.Padding(18, 12, 18, 12);
        this.panelBottom.Size = new System.Drawing.Size(934, 55);
        this.panelBottom.TabIndex = 3;
        // 
        // lblStepIndicator
        // 
        this.lblStepIndicator.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblStepIndicator.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblStepIndicator.Appearance.Options.UseFont = true;
        this.lblStepIndicator.Appearance.Options.UseForeColor = true;
        this.lblStepIndicator.Location = new System.Drawing.Point(24, 20);
        this.lblStepIndicator.Name = "lblStepIndicator";
        this.lblStepIndicator.Size = new System.Drawing.Size(59, 15);
        this.lblStepIndicator.TabIndex = 0;
        this.lblStepIndicator.Text = "Step 1 of 8";
        // 
        // lblFooterStatus
        // 
        this.lblFooterStatus.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.lblFooterStatus.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblFooterStatus.Appearance.Options.UseFont = true;
        this.lblFooterStatus.Appearance.Options.UseForeColor = true;
        this.lblFooterStatus.Location = new System.Drawing.Point(100, 20);
        this.lblFooterStatus.Name = "lblFooterStatus";
        this.lblFooterStatus.Size = new System.Drawing.Size(34, 13);
        this.lblFooterStatus.TabIndex = 1;
        this.lblFooterStatus.Text = "Ready.";
        // 
        // btnCancel
        // 
        this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnCancel.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnCancel.Appearance.Options.UseFont = true;
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(544, 12);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(88, 30);
        this.btnCancel.TabIndex = 2;
        this.btnCancel.Text = "Cancel";
        // 
        // btnBack
        // 
        this.btnBack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnBack.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnBack.Appearance.Options.UseFont = true;
        this.btnBack.Enabled = false;
        this.btnBack.Location = new System.Drawing.Point(640, 12);
        this.btnBack.Name = "btnBack";
        this.btnBack.Size = new System.Drawing.Size(88, 30);
        this.btnBack.TabIndex = 3;
        this.btnBack.Text = "< Back";
        // 
        // btnNext
        // 
        this.btnNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnNext.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnNext.Appearance.Options.UseFont = true;
        this.btnNext.Location = new System.Drawing.Point(736, 12);
        this.btnNext.Name = "btnNext";
        this.btnNext.Size = new System.Drawing.Size(88, 30);
        this.btnNext.TabIndex = 4;
        this.btnNext.Text = "Next >";
        // 
        // btnFinish
        // 
        this.btnFinish.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnFinish.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnFinish.Appearance.Options.UseFont = true;
        this.btnFinish.Location = new System.Drawing.Point(832, 12);
        this.btnFinish.Name = "btnFinish";
        this.btnFinish.Size = new System.Drawing.Size(90, 30);
        this.btnFinish.TabIndex = 5;
        this.btnFinish.Text = "Finish Setup";
        this.btnFinish.Visible = false;
        // 
        // panelContainer
        // 
        this.panelContainer.AutoScroll = true;
        this.panelContainer.Controls.Add(this.panelStep1);
        this.panelContainer.Controls.Add(this.panelStep2);
        this.panelContainer.Controls.Add(this.panelStep3);
        this.panelContainer.Controls.Add(this.panelStep4);
        this.panelContainer.Controls.Add(this.panelStep5);
        this.panelContainer.Controls.Add(this.panelStep6);
        this.panelContainer.Controls.Add(this.panelStep7);
        this.panelContainer.Controls.Add(this.panelStep8);
        this.panelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelContainer.Location = new System.Drawing.Point(240, 70);
        this.panelContainer.Name = "panelContainer";
        this.panelContainer.Padding = new System.Windows.Forms.Padding(24, 18, 24, 18);
        this.panelContainer.Size = new System.Drawing.Size(709, 526);
        this.panelContainer.TabIndex = 2;
        // 
        // panelStep1
        // 
        this.panelStep1.AutoScroll = true;
        this.panelStep1.Controls.Add(this.lblStep1Title);
        this.panelStep1.Controls.Add(this.lblStep1Desc);
        this.panelStep1.Controls.Add(this.grpPrerequisites);
        this.panelStep1.Controls.Add(this.grpSystemDetection);
        this.panelStep1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelStep1.Location = new System.Drawing.Point(24, 18);
        this.panelStep1.Name = "panelStep1";
        this.panelStep1.Size = new System.Drawing.Size(661, 490);
        this.panelStep1.TabIndex = 0;
        // 
        // lblStep1Title
        // 
        this.lblStep1Title.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblStep1Title.Appearance.Options.UseFont = true;
        this.lblStep1Title.Location = new System.Drawing.Point(0, 4);
        this.lblStep1Title.Name = "lblStep1Title";
        this.lblStep1Title.Size = new System.Drawing.Size(378, 21);
        this.lblStep1Title.TabIndex = 0;
        this.lblStep1Title.Text = "Welcome to Clovent Business Operating System";
        // 
        // lblStep1Desc
        // 
        this.lblStep1Desc.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep1Desc.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
        this.lblStep1Desc.Appearance.Options.UseFont = true;
        this.lblStep1Desc.Appearance.Options.UseForeColor = true;
        this.lblStep1Desc.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
        this.lblStep1Desc.Location = new System.Drawing.Point(0, 32);
        this.lblStep1Desc.Name = "lblStep1Desc";
        this.lblStep1Desc.Size = new System.Drawing.Size(560, 30);
        this.lblStep1Desc.TabIndex = 1;
        this.lblStep1Desc.Text = "This wizard prepares your workstation for production deployment. It configures database connectivity, applies schema migrations, defines your enterprise hierarchy, and provisions the initial administrator account.";
        this.lblStep1Desc.UseMnemonic = false;
        // 
        // grpPrerequisites
        // 
        this.grpPrerequisites.Controls.Add(this.lblPrereqSql);
        this.grpPrerequisites.Controls.Add(this.lblPrereqRuntime);
        this.grpPrerequisites.Controls.Add(this.lblPrereqDisplay);
        this.grpPrerequisites.Controls.Add(this.lblPrereqAdmin);
        this.grpPrerequisites.Location = new System.Drawing.Point(0, 80);
        this.grpPrerequisites.Name = "grpPrerequisites";
        this.grpPrerequisites.Size = new System.Drawing.Size(640, 140);
        this.grpPrerequisites.TabIndex = 2;
        this.grpPrerequisites.Text = "System Prerequisites Checklist";
        // 
        // lblPrereqSql
        // 
        this.lblPrereqSql.Location = new System.Drawing.Point(16, 32);
        this.lblPrereqSql.Name = "lblPrereqSql";
        this.lblPrereqSql.Size = new System.Drawing.Size(425, 15);
        this.lblPrereqSql.TabIndex = 0;
        this.lblPrereqSql.Text = "• Microsoft SQL Server 2019/2022 instance reachable (Local or Centralized Network" +
    ")";
        // 
        // lblPrereqRuntime
        // 
        this.lblPrereqRuntime.Location = new System.Drawing.Point(16, 56);
        this.lblPrereqRuntime.Name = "lblPrereqRuntime";
        this.lblPrereqRuntime.Size = new System.Drawing.Size(395, 15);
        this.lblPrereqRuntime.TabIndex = 1;
        this.lblPrereqRuntime.Text = "• .NET 10 Windows Desktop Runtime (x64) and DevExpress 26.1 WinForms";
        // 
        // lblPrereqDisplay
        // 
        this.lblPrereqDisplay.Location = new System.Drawing.Point(16, 80);
        this.lblPrereqDisplay.Name = "lblPrereqDisplay";
        this.lblPrereqDisplay.Size = new System.Drawing.Size(460, 15);
        this.lblPrereqDisplay.TabIndex = 2;
        this.lblPrereqDisplay.Text = "• Display resolution 1366×768 or higher (High-DPI PerMonitorV2 scaling supported)" +
    "";
        // 
        // lblPrereqAdmin
        // 
        this.lblPrereqAdmin.Location = new System.Drawing.Point(16, 104);
        this.lblPrereqAdmin.Name = "lblPrereqAdmin";
        this.lblPrereqAdmin.Size = new System.Drawing.Size(450, 15);
        this.lblPrereqAdmin.TabIndex = 3;
        this.lblPrereqAdmin.Text = "• Windows Administrator elevation (for ProgramData machine-wide configuration)";
        // 
        // grpSystemDetection
        // 
        this.grpSystemDetection.Controls.Add(this.lblDetectedOs);
        this.grpSystemDetection.Controls.Add(this.lblDetectedRuntime);
        this.grpSystemDetection.Controls.Add(this.lblDetectedDpi);
        this.grpSystemDetection.Controls.Add(this.lblDetectedElevation);
        this.grpSystemDetection.Location = new System.Drawing.Point(0, 235);
        this.grpSystemDetection.Name = "grpSystemDetection";
        this.grpSystemDetection.Size = new System.Drawing.Size(640, 140);
        this.grpSystemDetection.TabIndex = 3;
        this.grpSystemDetection.Text = "Detected Workstation Environment";
        // 
        // lblDetectedOs
        // 
        this.lblDetectedOs.Location = new System.Drawing.Point(16, 32);
        this.lblDetectedOs.Name = "lblDetectedOs";
        this.lblDetectedOs.Size = new System.Drawing.Size(120, 15);
        this.lblDetectedOs.TabIndex = 0;
        this.lblDetectedOs.Text = "Operating System: ...";
        // 
        // lblDetectedRuntime
        // 
        this.lblDetectedRuntime.Location = new System.Drawing.Point(16, 56);
        this.lblDetectedRuntime.Name = "lblDetectedRuntime";
        this.lblDetectedRuntime.Size = new System.Drawing.Size(100, 15);
        this.lblDetectedRuntime.TabIndex = 1;
        this.lblDetectedRuntime.Text = ".NET Runtime: ...";
        // 
        // lblDetectedDpi
        // 
        this.lblDetectedDpi.Location = new System.Drawing.Point(16, 80);
        this.lblDetectedDpi.Name = "lblDetectedDpi";
        this.lblDetectedDpi.Size = new System.Drawing.Size(115, 15);
        this.lblDetectedDpi.TabIndex = 2;
        this.lblDetectedDpi.Text = "Display DPI Scaling: ...";
        // 
        // lblDetectedElevation
        // 
        this.lblDetectedElevation.Location = new System.Drawing.Point(16, 104);
        this.lblDetectedElevation.Name = "lblDetectedElevation";
        this.lblDetectedElevation.Size = new System.Drawing.Size(130, 15);
        this.lblDetectedElevation.TabIndex = 3;
        this.lblDetectedElevation.Text = "Elevation Status: ...";
        // 
        // panelStep2
        // 
        this.panelStep2.AutoScroll = true;
        this.panelStep2.Controls.Add(this.lblStep2Title);
        this.panelStep2.Controls.Add(this.lblStep2Desc);
        this.panelStep2.Controls.Add(this.lblServer);
        this.panelStep2.Controls.Add(this.txtServer);
        this.panelStep2.Controls.Add(this.lblDatabase);
        this.panelStep2.Controls.Add(this.txtDatabase);
        this.panelStep2.Controls.Add(this.lblAuth);
        this.panelStep2.Controls.Add(this.cmbAuth);
        this.panelStep2.Controls.Add(this.lblUsername);
        this.panelStep2.Controls.Add(this.txtUsername);
        this.panelStep2.Controls.Add(this.lblPassword);
        this.panelStep2.Controls.Add(this.txtPassword);
        this.panelStep2.Controls.Add(this.btnTestConnection);
        this.panelStep2.Controls.Add(this.lblConnectionStatus);
        this.panelStep2.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelStep2.Location = new System.Drawing.Point(24, 18);
        this.panelStep2.Name = "panelStep2";
        this.panelStep2.Size = new System.Drawing.Size(661, 490);
        this.panelStep2.TabIndex = 1;
        this.panelStep2.Visible = false;
        // 
        // lblStep2Title
        // 
        this.lblStep2Title.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblStep2Title.Appearance.Options.UseFont = true;
        this.lblStep2Title.Location = new System.Drawing.Point(0, 4);
        this.lblStep2Title.Name = "lblStep2Title";
        this.lblStep2Title.Size = new System.Drawing.Size(235, 21);
        this.lblStep2Title.TabIndex = 0;
        this.lblStep2Title.Text = "Database Connection Settings";
        // 
        // lblStep2Desc
        // 
        this.lblStep2Desc.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep2Desc.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
        this.lblStep2Desc.Appearance.Options.UseFont = true;
        this.lblStep2Desc.Appearance.Options.UseForeColor = true;
        this.lblStep2Desc.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
        this.lblStep2Desc.Location = new System.Drawing.Point(0, 32);
        this.lblStep2Desc.Name = "lblStep2Desc";
        this.lblStep2Desc.Size = new System.Drawing.Size(640, 32);
        this.lblStep2Desc.TabIndex = 1;
        this.lblStep2Desc.Text = "Specify the SQL Server instance and database name. You must test the connection successfully before proceeding.";
        this.lblStep2Desc.UseMnemonic = false;
        // 
        // lblServer
        // 
        this.lblServer.Location = new System.Drawing.Point(0, 76);
        this.lblServer.Name = "lblServer";
        this.lblServer.Size = new System.Drawing.Size(94, 15);
        this.lblServer.TabIndex = 2;
        this.lblServer.Text = "SQL Server / Host:";
        // 
        // txtServer
        // 
        this.txtServer.EditValue = ".";
        this.txtServer.Location = new System.Drawing.Point(0, 96);
        this.txtServer.Name = "txtServer";
        this.txtServer.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtServer.Properties.Appearance.Options.UseFont = true;
        this.txtServer.Size = new System.Drawing.Size(460, 24);
        this.txtServer.TabIndex = 3;
        // 
        // lblDatabase
        // 
        this.lblDatabase.Location = new System.Drawing.Point(0, 130);
        this.lblDatabase.Name = "lblDatabase";
        this.lblDatabase.Size = new System.Drawing.Size(87, 15);
        this.lblDatabase.TabIndex = 4;
        this.lblDatabase.Text = "Database Name:";
        // 
        // txtDatabase
        // 
        this.txtDatabase.EditValue = "Clovent_BusinessOperatingSystem";
        this.txtDatabase.Location = new System.Drawing.Point(0, 150);
        this.txtDatabase.Name = "txtDatabase";
        this.txtDatabase.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtDatabase.Properties.Appearance.Options.UseFont = true;
        this.txtDatabase.Size = new System.Drawing.Size(460, 24);
        this.txtDatabase.TabIndex = 5;
        // 
        // lblAuth
        // 
        this.lblAuth.Location = new System.Drawing.Point(0, 184);
        this.lblAuth.Name = "lblAuth";
        this.lblAuth.Size = new System.Drawing.Size(107, 15);
        this.lblAuth.TabIndex = 6;
        this.lblAuth.Text = "Authentication Type:";
        // 
        // cmbAuth
        // 
        this.cmbAuth.Location = new System.Drawing.Point(0, 204);
        this.cmbAuth.Name = "cmbAuth";
        this.cmbAuth.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.cmbAuth.Properties.Appearance.Options.UseFont = true;
        this.cmbAuth.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbAuth.Properties.Items.AddRange(new object[] {
            "Windows Authentication (Integrated Security - Recommended)",
            "SQL Server Authentication (Encrypted Credentials)"});
        this.cmbAuth.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbAuth.Size = new System.Drawing.Size(460, 24);
        this.cmbAuth.TabIndex = 7;
        // 
        // lblUsername
        // 
        this.lblUsername.Enabled = false;
        this.lblUsername.Location = new System.Drawing.Point(0, 238);
        this.lblUsername.Name = "lblUsername";
        this.lblUsername.Size = new System.Drawing.Size(81, 15);
        this.lblUsername.TabIndex = 8;
        this.lblUsername.Text = "SQL Username:";
        // 
        // txtUsername
        // 
        this.txtUsername.EditValue = "cbos_app";
        this.txtUsername.Enabled = false;
        this.txtUsername.Location = new System.Drawing.Point(0, 258);
        this.txtUsername.Name = "txtUsername";
        this.txtUsername.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtUsername.Properties.Appearance.Options.UseFont = true;
        this.txtUsername.Size = new System.Drawing.Size(460, 24);
        this.txtUsername.TabIndex = 9;
        // 
        // lblPassword
        // 
        this.lblPassword.Enabled = false;
        this.lblPassword.Location = new System.Drawing.Point(0, 292);
        this.lblPassword.Name = "lblPassword";
        this.lblPassword.Size = new System.Drawing.Size(77, 15);
        this.lblPassword.TabIndex = 10;
        this.lblPassword.Text = "SQL Password:";
        // 
        // txtPassword
        // 
        this.txtPassword.Enabled = false;
        this.txtPassword.Location = new System.Drawing.Point(0, 312);
        this.txtPassword.Name = "txtPassword";
        this.txtPassword.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtPassword.Properties.Appearance.Options.UseFont = true;
        this.txtPassword.Properties.UseSystemPasswordChar = true;
        this.txtPassword.Size = new System.Drawing.Size(460, 24);
        this.txtPassword.TabIndex = 11;
        // 
        // btnTestConnection
        // 
        this.btnTestConnection.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnTestConnection.Appearance.Options.UseFont = true;
        this.btnTestConnection.Location = new System.Drawing.Point(0, 352);
        this.btnTestConnection.Name = "btnTestConnection";
        this.btnTestConnection.Size = new System.Drawing.Size(140, 30);
        this.btnTestConnection.TabIndex = 12;
        this.btnTestConnection.Text = "Test Connection";
        // 
        // lblConnectionStatus
        // 
        this.lblConnectionStatus.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblConnectionStatus.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblConnectionStatus.Appearance.Options.UseFont = true;
        this.lblConnectionStatus.Appearance.Options.UseForeColor = true;
        this.lblConnectionStatus.Location = new System.Drawing.Point(155, 359);
        this.lblConnectionStatus.Name = "lblConnectionStatus";
        this.lblConnectionStatus.Size = new System.Drawing.Size(183, 15);
        this.lblConnectionStatus.TabIndex = 13;
        this.lblConnectionStatus.Text = "Connection not yet tested.";
        // 
        // panelStep3
        // 
        this.panelStep3.AutoScroll = true;
        this.panelStep3.Controls.Add(this.lblStep3Title);
        this.panelStep3.Controls.Add(this.lblStep3Desc);
        this.panelStep3.Controls.Add(this.chkCreateDbIfMissing);
        this.panelStep3.Controls.Add(this.chkApplyMigrations);
        this.panelStep3.Controls.Add(this.btnApplyMigrations);
        this.panelStep3.Controls.Add(this.progressMigrations);
        this.panelStep3.Controls.Add(this.lblMigrationStatus);
        this.panelStep3.Controls.Add(this.memoMigrationLog);
        this.panelStep3.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelStep3.Location = new System.Drawing.Point(24, 18);
        this.panelStep3.Name = "panelStep3";
        this.panelStep3.Size = new System.Drawing.Size(661, 490);
        this.panelStep3.TabIndex = 2;
        this.panelStep3.Visible = false;
        // 
        // lblStep3Title
        // 
        this.lblStep3Title.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblStep3Title.Appearance.Options.UseFont = true;
        this.lblStep3Title.Location = new System.Drawing.Point(0, 4);
        this.lblStep3Title.Name = "lblStep3Title";
        this.lblStep3Title.Size = new System.Drawing.Size(325, 21);
        this.lblStep3Title.TabIndex = 0;
        this.lblStep3Title.Text = "Database Initialization && EF Core Migrations";
        // 
        // lblStep3Desc
        // 
        this.lblStep3Desc.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep3Desc.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
        this.lblStep3Desc.Appearance.Options.UseFont = true;
        this.lblStep3Desc.Appearance.Options.UseForeColor = true;
        this.lblStep3Desc.Location = new System.Drawing.Point(0, 32);
        this.lblStep3Desc.Name = "lblStep3Desc";
        this.lblStep3Desc.Size = new System.Drawing.Size(575, 15);
        this.lblStep3Desc.TabIndex = 1;
        this.lblStep3Desc.Text = "Initialize the physical database and apply EF Core migrations sequentially across" +
    " all 6 bounded contexts.";
        // 
        // chkCreateDbIfMissing
        // 
        this.chkCreateDbIfMissing.EditValue = true;
        this.chkCreateDbIfMissing.Location = new System.Drawing.Point(0, 65);
        this.chkCreateDbIfMissing.Name = "chkCreateDbIfMissing";
        this.chkCreateDbIfMissing.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkCreateDbIfMissing.Properties.Appearance.Options.UseFont = true;
        this.chkCreateDbIfMissing.Properties.Caption = "Create database if missing (Clean / Fresh Installation)";
        this.chkCreateDbIfMissing.Size = new System.Drawing.Size(460, 20);
        this.chkCreateDbIfMissing.TabIndex = 2;
        // 
        // chkApplyMigrations
        // 
        this.chkApplyMigrations.EditValue = true;
        this.chkApplyMigrations.Location = new System.Drawing.Point(0, 92);
        this.chkApplyMigrations.Name = "chkApplyMigrations";
        this.chkApplyMigrations.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkApplyMigrations.Properties.Appearance.Options.UseFont = true;
        this.chkApplyMigrations.Properties.Caption = "Apply EF Core migrations across all 6 bounded-context schemas";
        this.chkApplyMigrations.Size = new System.Drawing.Size(640, 20);
        this.chkApplyMigrations.TabIndex = 3;
        // 
        // btnApplyMigrations
        // 
        this.btnApplyMigrations.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnApplyMigrations.Appearance.Options.UseFont = true;
        this.btnApplyMigrations.Location = new System.Drawing.Point(0, 126);
        this.btnApplyMigrations.Name = "btnApplyMigrations";
        this.btnApplyMigrations.Size = new System.Drawing.Size(185, 32);
        this.btnApplyMigrations.TabIndex = 4;
        this.btnApplyMigrations.Text = "Apply Database Migrations";
        // 
        // progressMigrations
        // 
        this.progressMigrations.Location = new System.Drawing.Point(0, 172);
        this.progressMigrations.Name = "progressMigrations";
        this.progressMigrations.Properties.ShowTitle = true;
        this.progressMigrations.Size = new System.Drawing.Size(640, 20);
        this.progressMigrations.TabIndex = 5;
        // 
        // lblMigrationStatus
        // 
        this.lblMigrationStatus.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblMigrationStatus.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblMigrationStatus.Appearance.Options.UseFont = true;
        this.lblMigrationStatus.Appearance.Options.UseForeColor = true;
        this.lblMigrationStatus.Location = new System.Drawing.Point(0, 198);
        this.lblMigrationStatus.Name = "lblMigrationStatus";
        this.lblMigrationStatus.Size = new System.Drawing.Size(176, 15);
        this.lblMigrationStatus.TabIndex = 6;
        this.lblMigrationStatus.Text = "Ready to initialize and migrate.";
        // 
        // memoMigrationLog
        // 
        this.memoMigrationLog.Location = new System.Drawing.Point(0, 224);
        this.memoMigrationLog.Name = "memoMigrationLog";
        this.memoMigrationLog.Properties.Appearance.Font = new System.Drawing.Font("Consolas", 8.5F);
        this.memoMigrationLog.Properties.Appearance.Options.UseFont = true;
        this.memoMigrationLog.Properties.ReadOnly = true;
        this.memoMigrationLog.Properties.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this.memoMigrationLog.Size = new System.Drawing.Size(640, 180);
        this.memoMigrationLog.TabIndex = 7;
        // 
        // panelStep4
        // 
        this.panelStep4.AutoScroll = true;
        this.panelStep4.Controls.Add(this.lblStep4Title);
        this.panelStep4.Controls.Add(this.lblStep4Desc);
        this.panelStep4.Controls.Add(this.lblOrgName);
        this.panelStep4.Controls.Add(this.txtOrgName);
        this.panelStep4.Controls.Add(this.lblTaxId);
        this.panelStep4.Controls.Add(this.txtTaxId);
        this.panelStep4.Controls.Add(this.lblCompanyName);
        this.panelStep4.Controls.Add(this.txtCompanyName);
        this.panelStep4.Controls.Add(this.lblBranchName);
        this.panelStep4.Controls.Add(this.txtBranchName);
        this.panelStep4.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelStep4.Location = new System.Drawing.Point(24, 18);
        this.panelStep4.Name = "panelStep4";
        this.panelStep4.Size = new System.Drawing.Size(661, 490);
        this.panelStep4.TabIndex = 3;
        this.panelStep4.Visible = false;
        // 
        // lblStep4Title
        // 
        this.lblStep4Title.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblStep4Title.Appearance.Options.UseFont = true;
        this.lblStep4Title.Location = new System.Drawing.Point(0, 4);
        this.lblStep4Title.Name = "lblStep4Title";
        this.lblStep4Title.Size = new System.Drawing.Size(262, 21);
        this.lblStep4Title.TabIndex = 0;
        this.lblStep4Title.Text = "Organization && Branch Hierarchy";
        // 
        // lblStep4Desc
        // 
        this.lblStep4Desc.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep4Desc.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
        this.lblStep4Desc.Appearance.Options.UseFont = true;
        this.lblStep4Desc.Appearance.Options.UseForeColor = true;
        this.lblStep4Desc.Location = new System.Drawing.Point(0, 32);
        this.lblStep4Desc.Name = "lblStep4Desc";
        this.lblStep4Desc.Size = new System.Drawing.Size(534, 15);
        this.lblStep4Desc.TabIndex = 1;
        this.lblStep4Desc.Text = "Define your parent organization, primary operating company, and initial branch or" +
    " store location.";
        // 
        // lblOrgName
        // 
        this.lblOrgName.Location = new System.Drawing.Point(0, 68);
        this.lblOrgName.Name = "lblOrgName";
        this.lblOrgName.Size = new System.Drawing.Size(220, 15);
        this.lblOrgName.TabIndex = 2;
        this.lblOrgName.Text = "Organization Legal Name (Enterprise / Group):";
        // 
        // txtOrgName
        // 
        this.txtOrgName.EditValue = "Clovent Business Solutions";
        this.txtOrgName.Location = new System.Drawing.Point(0, 88);
        this.txtOrgName.Name = "txtOrgName";
        this.txtOrgName.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtOrgName.Properties.Appearance.Options.UseFont = true;
        this.txtOrgName.Size = new System.Drawing.Size(460, 24);
        this.txtOrgName.TabIndex = 3;
        // 
        // lblTaxId
        // 
        this.lblTaxId.Location = new System.Drawing.Point(0, 126);
        this.lblTaxId.Name = "lblTaxId";
        this.lblTaxId.Size = new System.Drawing.Size(200, 15);
        this.lblTaxId.TabIndex = 4;
        this.lblTaxId.Text = "Organization Tax ID / Code (Optional):";
        // 
        // txtTaxId
        // 
        this.txtTaxId.EditValue = "ORG-001";
        this.txtTaxId.Location = new System.Drawing.Point(0, 146);
        this.txtTaxId.Name = "txtTaxId";
        this.txtTaxId.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtTaxId.Properties.Appearance.Options.UseFont = true;
        this.txtTaxId.Size = new System.Drawing.Size(460, 24);
        this.txtTaxId.TabIndex = 5;
        // 
        // lblCompanyName
        // 
        this.lblCompanyName.Location = new System.Drawing.Point(0, 184);
        this.lblCompanyName.Name = "lblCompanyName";
        this.lblCompanyName.Size = new System.Drawing.Size(142, 15);
        this.lblCompanyName.TabIndex = 6;
        this.lblCompanyName.Text = "Operating Company Name:";
        // 
        // txtCompanyName
        // 
        this.txtCompanyName.EditValue = "Headquarters Company";
        this.txtCompanyName.Location = new System.Drawing.Point(0, 204);
        this.txtCompanyName.Name = "txtCompanyName";
        this.txtCompanyName.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtCompanyName.Properties.Appearance.Options.UseFont = true;
        this.txtCompanyName.Size = new System.Drawing.Size(460, 24);
        this.txtCompanyName.TabIndex = 7;
        // 
        // lblBranchName
        // 
        this.lblBranchName.Location = new System.Drawing.Point(0, 242);
        this.lblBranchName.Name = "lblBranchName";
        this.lblBranchName.Size = new System.Drawing.Size(143, 15);
        this.lblBranchName.TabIndex = 8;
        this.lblBranchName.Text = "Store / Branch Location Name:";
        // 
        // txtBranchName
        // 
        this.txtBranchName.EditValue = "Main Branch";
        this.txtBranchName.Location = new System.Drawing.Point(0, 262);
        this.txtBranchName.Name = "txtBranchName";
        this.txtBranchName.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtBranchName.Properties.Appearance.Options.UseFont = true;
        this.txtBranchName.Size = new System.Drawing.Size(460, 24);
        this.txtBranchName.TabIndex = 9;
        // 
        // panelStep5
        // 
        this.panelStep5.AutoScroll = true;
        this.panelStep5.Controls.Add(this.lblStep5Title);
        this.panelStep5.Controls.Add(this.lblStep5Desc);
        this.panelStep5.Controls.Add(this.lblAdminUsername);
        this.panelStep5.Controls.Add(this.txtAdminUsername);
        this.panelStep5.Controls.Add(this.lblAdminFullName);
        this.panelStep5.Controls.Add(this.txtAdminFullName);
        this.panelStep5.Controls.Add(this.lblAdminEmail);
        this.panelStep5.Controls.Add(this.txtAdminEmail);
        this.panelStep5.Controls.Add(this.lblAdminPassword);
        this.panelStep5.Controls.Add(this.txtAdminPassword);
        this.panelStep5.Controls.Add(this.lblAdminConfirmPassword);
        this.panelStep5.Controls.Add(this.txtAdminConfirmPassword);
        this.panelStep5.Controls.Add(this.lblPasswordStrength);
        this.panelStep5.Controls.Add(this.progressPasswordStrength);
        this.panelStep5.Controls.Add(this.lblPasswordPolicy);
        this.panelStep5.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelStep5.Location = new System.Drawing.Point(24, 18);
        this.panelStep5.Name = "panelStep5";
        this.panelStep5.Size = new System.Drawing.Size(661, 490);
        this.panelStep5.TabIndex = 4;
        this.panelStep5.Visible = false;
        // 
        // lblStep5Title
        // 
        this.lblStep5Title.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblStep5Title.Appearance.Options.UseFont = true;
        this.lblStep5Title.Location = new System.Drawing.Point(0, 4);
        this.lblStep5Title.Name = "lblStep5Title";
        this.lblStep5Title.Size = new System.Drawing.Size(217, 21);
        this.lblStep5Title.TabIndex = 0;
        this.lblStep5Title.Text = "First Administrator Account";
        // 
        // lblStep5Desc
        // 
        this.lblStep5Desc.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep5Desc.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
        this.lblStep5Desc.Appearance.Options.UseFont = true;
        this.lblStep5Desc.Appearance.Options.UseForeColor = true;
        this.lblStep5Desc.Location = new System.Drawing.Point(0, 32);
        this.lblStep5Desc.Name = "lblStep5Desc";
        this.lblStep5Desc.Size = new System.Drawing.Size(564, 15);
        this.lblStep5Desc.TabIndex = 1;
        this.lblStep5Desc.Text = "Provision the initial system administrator. This account receives full security a" +
    "nd configuration permissions.";
        // 
        // lblAdminUsername
        // 
        this.lblAdminUsername.Location = new System.Drawing.Point(0, 60);
        this.lblAdminUsername.Name = "lblAdminUsername";
        this.lblAdminUsername.Size = new System.Drawing.Size(56, 15);
        this.lblAdminUsername.TabIndex = 2;
        this.lblAdminUsername.Text = "Username:";
        // 
        // txtAdminUsername
        // 
        this.txtAdminUsername.EditValue = "admin";
        this.txtAdminUsername.Location = new System.Drawing.Point(0, 78);
        this.txtAdminUsername.Name = "txtAdminUsername";
        this.txtAdminUsername.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtAdminUsername.Properties.Appearance.Options.UseFont = true;
        this.txtAdminUsername.Size = new System.Drawing.Size(360, 24);
        this.txtAdminUsername.TabIndex = 3;
        // 
        // lblAdminFullName
        // 
        this.lblAdminFullName.Location = new System.Drawing.Point(0, 110);
        this.lblAdminFullName.Name = "lblAdminFullName";
        this.lblAdminFullName.Size = new System.Drawing.Size(57, 15);
        this.lblAdminFullName.TabIndex = 4;
        this.lblAdminFullName.Text = "Full Name:";
        // 
        // txtAdminFullName
        // 
        this.txtAdminFullName.EditValue = "System Administrator";
        this.txtAdminFullName.Location = new System.Drawing.Point(0, 128);
        this.txtAdminFullName.Name = "txtAdminFullName";
        this.txtAdminFullName.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtAdminFullName.Properties.Appearance.Options.UseFont = true;
        this.txtAdminFullName.Size = new System.Drawing.Size(360, 24);
        this.txtAdminFullName.TabIndex = 5;
        // 
        // lblAdminEmail
        // 
        this.lblAdminEmail.Location = new System.Drawing.Point(0, 160);
        this.lblAdminEmail.Name = "lblAdminEmail";
        this.lblAdminEmail.Size = new System.Drawing.Size(77, 15);
        this.lblAdminEmail.TabIndex = 6;
        this.lblAdminEmail.Text = "Email Address:";
        // 
        // txtAdminEmail
        // 
        this.txtAdminEmail.EditValue = "admin@clovent.local";
        this.txtAdminEmail.Location = new System.Drawing.Point(0, 178);
        this.txtAdminEmail.Name = "txtAdminEmail";
        this.txtAdminEmail.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtAdminEmail.Properties.Appearance.Options.UseFont = true;
        this.txtAdminEmail.Size = new System.Drawing.Size(360, 24);
        this.txtAdminEmail.TabIndex = 7;
        // 
        // lblAdminPassword
        // 
        this.lblAdminPassword.Location = new System.Drawing.Point(0, 210);
        this.lblAdminPassword.Name = "lblAdminPassword";
        this.lblAdminPassword.Size = new System.Drawing.Size(89, 15);
        this.lblAdminPassword.TabIndex = 8;
        this.lblAdminPassword.Text = "Secure Password:";
        // 
        // txtAdminPassword
        // 
        this.txtAdminPassword.Location = new System.Drawing.Point(0, 228);
        this.txtAdminPassword.Name = "txtAdminPassword";
        this.txtAdminPassword.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtAdminPassword.Properties.Appearance.Options.UseFont = true;
        this.txtAdminPassword.Properties.UseSystemPasswordChar = true;
        this.txtAdminPassword.Size = new System.Drawing.Size(360, 24);
        this.txtAdminPassword.TabIndex = 9;
        // 
        // lblAdminConfirmPassword
        // 
        this.lblAdminConfirmPassword.Location = new System.Drawing.Point(0, 260);
        this.lblAdminConfirmPassword.Name = "lblAdminConfirmPassword";
        this.lblAdminConfirmPassword.Size = new System.Drawing.Size(100, 15);
        this.lblAdminConfirmPassword.TabIndex = 10;
        this.lblAdminConfirmPassword.Text = "Confirm Password:";
        // 
        // txtAdminConfirmPassword
        // 
        this.txtAdminConfirmPassword.Location = new System.Drawing.Point(0, 278);
        this.txtAdminConfirmPassword.Name = "txtAdminConfirmPassword";
        this.txtAdminConfirmPassword.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtAdminConfirmPassword.Properties.Appearance.Options.UseFont = true;
        this.txtAdminConfirmPassword.Properties.UseSystemPasswordChar = true;
        this.txtAdminConfirmPassword.Size = new System.Drawing.Size(360, 24);
        this.txtAdminConfirmPassword.TabIndex = 11;
        // 
        // lblPasswordStrength
        // 
        this.lblPasswordStrength.Location = new System.Drawing.Point(0, 310);
        this.lblPasswordStrength.Name = "lblPasswordStrength";
        this.lblPasswordStrength.Size = new System.Drawing.Size(142, 15);
        this.lblPasswordStrength.TabIndex = 12;
        this.lblPasswordStrength.Text = "Password Strength: (empty)";
        // 
        // progressPasswordStrength
        // 
        this.progressPasswordStrength.Location = new System.Drawing.Point(0, 330);
        this.progressPasswordStrength.Name = "progressPasswordStrength";
        this.progressPasswordStrength.Size = new System.Drawing.Size(360, 12);
        this.progressPasswordStrength.TabIndex = 13;
        // 
        // lblPasswordPolicy
        // 
        this.lblPasswordPolicy.Appearance.Font = new System.Drawing.Font("Segoe UI", 8F);
        this.lblPasswordPolicy.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblPasswordPolicy.Appearance.Options.UseFont = true;
        this.lblPasswordPolicy.Appearance.Options.UseForeColor = true;
        this.lblPasswordPolicy.Location = new System.Drawing.Point(0, 350);
        this.lblPasswordPolicy.Name = "lblPasswordPolicy";
        this.lblPasswordPolicy.Size = new System.Drawing.Size(434, 13);
        this.lblPasswordPolicy.TabIndex = 14;
        this.lblPasswordPolicy.Text = "Policy: Minimum 8 characters. Must contain letters and digits. Default passwords" +
    " prohibited.";
        // 
        // panelStep6
        // 
        this.panelStep6.AutoScroll = true;
        this.panelStep6.Controls.Add(this.lblStep6Title);
        this.panelStep6.Controls.Add(this.lblStep6Desc);
        this.panelStep6.Controls.Add(this.lblTimeZone);
        this.panelStep6.Controls.Add(this.cmbTimeZone);
        this.panelStep6.Controls.Add(this.lblDateFormat);
        this.panelStep6.Controls.Add(this.cmbDateFormat);
        this.panelStep6.Controls.Add(this.lblTimeFormat);
        this.panelStep6.Controls.Add(this.cmbTimeFormat);
        this.panelStep6.Controls.Add(this.lblCurrency);
        this.panelStep6.Controls.Add(this.cmbCurrency);
        this.panelStep6.Controls.Add(this.lblTerminalName);
        this.panelStep6.Controls.Add(this.txtTerminalName);
        this.panelStep6.Controls.Add(this.lblSamplePreview);
        this.panelStep6.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelStep6.Location = new System.Drawing.Point(24, 18);
        this.panelStep6.Name = "panelStep6";
        this.panelStep6.Size = new System.Drawing.Size(661, 490);
        this.panelStep6.TabIndex = 5;
        this.panelStep6.Visible = false;
        // 
        // lblStep6Title
        // 
        this.lblStep6Title.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblStep6Title.Appearance.Options.UseFont = true;
        this.lblStep6Title.Location = new System.Drawing.Point(0, 4);
        this.lblStep6Title.Name = "lblStep6Title";
        this.lblStep6Title.Size = new System.Drawing.Size(209, 21);
        this.lblStep6Title.TabIndex = 0;
        this.lblStep6Title.Text = "Business && Regional Settings";
        // 
        // lblStep6Desc
        // 
        this.lblStep6Desc.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep6Desc.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
        this.lblStep6Desc.Appearance.Options.UseFont = true;
        this.lblStep6Desc.Appearance.Options.UseForeColor = true;
        this.lblStep6Desc.Location = new System.Drawing.Point(0, 32);
        this.lblStep6Desc.Name = "lblStep6Desc";
        this.lblStep6Desc.Size = new System.Drawing.Size(465, 15);
        this.lblStep6Desc.TabIndex = 1;
        this.lblStep6Desc.Text = "Configure operating time zone, formatting patterns, base currency, and workstation name.";
        // 
        // lblTimeZone
        // 
        this.lblTimeZone.Location = new System.Drawing.Point(0, 65);
        this.lblTimeZone.Name = "lblTimeZone";
        this.lblTimeZone.Size = new System.Drawing.Size(59, 15);
        this.lblTimeZone.TabIndex = 2;
        this.lblTimeZone.Text = "Time Zone:";
        // 
        // cmbTimeZone
        // 
        this.cmbTimeZone.Location = new System.Drawing.Point(0, 85);
        this.cmbTimeZone.Name = "cmbTimeZone";
        this.cmbTimeZone.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.cmbTimeZone.Properties.Appearance.Options.UseFont = true;
        this.cmbTimeZone.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbTimeZone.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbTimeZone.Size = new System.Drawing.Size(460, 24);
        this.cmbTimeZone.TabIndex = 3;
        // 
        // lblDateFormat
        // 
        this.lblDateFormat.Location = new System.Drawing.Point(0, 120);
        this.lblDateFormat.Name = "lblDateFormat";
        this.lblDateFormat.Size = new System.Drawing.Size(68, 15);
        this.lblDateFormat.TabIndex = 4;
        this.lblDateFormat.Text = "Date Format:";
        // 
        // cmbDateFormat
        // 
        this.cmbDateFormat.Location = new System.Drawing.Point(0, 140);
        this.cmbDateFormat.Name = "cmbDateFormat";
        this.cmbDateFormat.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.cmbDateFormat.Properties.Appearance.Options.UseFont = true;
        this.cmbDateFormat.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbDateFormat.Properties.Items.AddRange(new object[] {
            "dd-MMM-yyyy",
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy"});
        this.cmbDateFormat.Size = new System.Drawing.Size(225, 24);
        this.cmbDateFormat.TabIndex = 5;
        // 
        // lblTimeFormat
        // 
        this.lblTimeFormat.Location = new System.Drawing.Point(235, 120);
        this.lblTimeFormat.Name = "lblTimeFormat";
        this.lblTimeFormat.Size = new System.Drawing.Size(70, 15);
        this.lblTimeFormat.TabIndex = 6;
        this.lblTimeFormat.Text = "Time Format:";
        // 
        // cmbTimeFormat
        // 
        this.cmbTimeFormat.Location = new System.Drawing.Point(235, 140);
        this.cmbTimeFormat.Name = "cmbTimeFormat";
        this.cmbTimeFormat.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.cmbTimeFormat.Properties.Appearance.Options.UseFont = true;
        this.cmbTimeFormat.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbTimeFormat.Properties.Items.AddRange(new object[] {
            "12 Hour (hh:mm tt)",
            "24 Hour (HH:mm)"});
        this.cmbTimeFormat.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbTimeFormat.Size = new System.Drawing.Size(225, 24);
        this.cmbTimeFormat.TabIndex = 7;
        // 
        // lblCurrency
        // 
        this.lblCurrency.Location = new System.Drawing.Point(0, 175);
        this.lblCurrency.Name = "lblCurrency";
        this.lblCurrency.Size = new System.Drawing.Size(78, 15);
        this.lblCurrency.TabIndex = 8;
        this.lblCurrency.Text = "Base Currency:";
        // 
        // cmbCurrency
        // 
        this.cmbCurrency.Location = new System.Drawing.Point(0, 195);
        this.cmbCurrency.Name = "cmbCurrency";
        this.cmbCurrency.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.cmbCurrency.Properties.Appearance.Options.UseFont = true;
        this.cmbCurrency.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbCurrency.Properties.Items.AddRange(new object[] {
            "USD ($) - US Dollar",
            "EUR (€) - Euro",
            "GBP (£) - British Pound",
            "PKR (Rs.) - Pakistani Rupee",
            "CAD ($) - Canadian Dollar",
            "AUD ($) - Australian Dollar",
            "AED (د.إ) - UAE Dirham",
            "SAR (ر.س) - Saudi Riyal"});
        this.cmbCurrency.Size = new System.Drawing.Size(460, 24);
        this.cmbCurrency.TabIndex = 9;
        // 
        // lblTerminalName
        // 
        this.lblTerminalName.Location = new System.Drawing.Point(0, 230);
        this.lblTerminalName.Name = "lblTerminalName";
        this.lblTerminalName.Size = new System.Drawing.Size(126, 15);
        this.lblTerminalName.TabIndex = 10;
        this.lblTerminalName.Text = "Workstation / Terminal:";
        // 
        // txtTerminalName
        // 
        this.txtTerminalName.EditValue = "Front Counter";
        this.txtTerminalName.Location = new System.Drawing.Point(0, 250);
        this.txtTerminalName.Name = "txtTerminalName";
        this.txtTerminalName.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtTerminalName.Properties.Appearance.Options.UseFont = true;
        this.txtTerminalName.Size = new System.Drawing.Size(460, 24);
        this.txtTerminalName.TabIndex = 11;
        // 
        // lblSamplePreview
        // 
        this.lblSamplePreview.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
        this.lblSamplePreview.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
        this.lblSamplePreview.Appearance.Options.UseFont = true;
        this.lblSamplePreview.Appearance.Options.UseForeColor = true;
        this.lblSamplePreview.Location = new System.Drawing.Point(0, 290);
        this.lblSamplePreview.Name = "lblSamplePreview";
        this.lblSamplePreview.Size = new System.Drawing.Size(95, 15);
        this.lblSamplePreview.TabIndex = 12;
        this.lblSamplePreview.Text = "Preview: (Sample)";
        // 
        // panelStep7
        // 
        this.panelStep7.AutoScroll = true;
        this.panelStep7.Controls.Add(this.lblStep7Title);
        this.panelStep7.Controls.Add(this.lblStep7Desc);
        this.panelStep7.Controls.Add(this.lblHardwareId);
        this.panelStep7.Controls.Add(this.txtHardwareId);
        this.panelStep7.Controls.Add(this.btnCopyHardwareId);
        this.panelStep7.Controls.Add(this.btnImportLicense);
        this.panelStep7.Controls.Add(this.lblLicenseStatus);
        this.panelStep7.Controls.Add(this.lblLicensedTo);
        this.panelStep7.Controls.Add(this.chkEvaluationMode);
        this.panelStep7.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelStep7.Location = new System.Drawing.Point(24, 18);
        this.panelStep7.Name = "panelStep7";
        this.panelStep7.Size = new System.Drawing.Size(661, 490);
        this.panelStep7.TabIndex = 6;
        this.panelStep7.Visible = false;
        // 
        // lblStep7Title
        // 
        this.lblStep7Title.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblStep7Title.Appearance.Options.UseFont = true;
        this.lblStep7Title.Location = new System.Drawing.Point(0, 4);
        this.lblStep7Title.Name = "lblStep7Title";
        this.lblStep7Title.Size = new System.Drawing.Size(262, 21);
        this.lblStep7Title.TabIndex = 0;
        this.lblStep7Title.Text = "Software Registration && Licensing";
        // 
        // lblStep7Desc
        // 
        this.lblStep7Desc.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep7Desc.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
        this.lblStep7Desc.Appearance.Options.UseFont = true;
        this.lblStep7Desc.Appearance.Options.UseForeColor = true;
        this.lblStep7Desc.Location = new System.Drawing.Point(0, 32);
        this.lblStep7Desc.Name = "lblStep7Desc";
        this.lblStep7Desc.Size = new System.Drawing.Size(567, 15);
        this.lblStep7Desc.TabIndex = 1;
        this.lblStep7Desc.Text = "Import a cryptographically signed license file (*.lic), or continue in the 30-day" +
    " Evaluation / Trial mode.";
        // 
        // lblHardwareId
        // 
        this.lblHardwareId.Location = new System.Drawing.Point(0, 68);
        this.lblHardwareId.Name = "lblHardwareId";
        this.lblHardwareId.Size = new System.Drawing.Size(127, 15);
        this.lblHardwareId.TabIndex = 2;
        this.lblHardwareId.Text = "Workstation Hardware ID:";
        // 
        // txtHardwareId
        // 
        this.txtHardwareId.Location = new System.Drawing.Point(0, 88);
        this.txtHardwareId.Name = "txtHardwareId";
        this.txtHardwareId.Properties.Appearance.Font = new System.Drawing.Font("Consolas", 9.5F);
        this.txtHardwareId.Properties.Appearance.Options.UseFont = true;
        this.txtHardwareId.Properties.ReadOnly = true;
        this.txtHardwareId.Size = new System.Drawing.Size(340, 24);
        this.txtHardwareId.TabIndex = 3;
        // 
        // btnCopyHardwareId
        // 
        this.btnCopyHardwareId.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnCopyHardwareId.Appearance.Options.UseFont = true;
        this.btnCopyHardwareId.Location = new System.Drawing.Point(346, 88);
        this.btnCopyHardwareId.Name = "btnCopyHardwareId";
        this.btnCopyHardwareId.Size = new System.Drawing.Size(114, 24);
        this.btnCopyHardwareId.TabIndex = 4;
        this.btnCopyHardwareId.Text = "Copy Hardware ID";
        // 
        // btnImportLicense
        // 
        this.btnImportLicense.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnImportLicense.Appearance.Options.UseFont = true;
        this.btnImportLicense.Location = new System.Drawing.Point(0, 130);
        this.btnImportLicense.Name = "btnImportLicense";
        this.btnImportLicense.Size = new System.Drawing.Size(160, 30);
        this.btnImportLicense.TabIndex = 5;
        this.btnImportLicense.Text = "Import License File (.lic)...";
        // 
        // lblLicenseStatus
        // 
        this.lblLicenseStatus.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        this.lblLicenseStatus.Appearance.ForeColor = System.Drawing.Color.DarkOrange;
        this.lblLicenseStatus.Appearance.Options.UseFont = true;
        this.lblLicenseStatus.Appearance.Options.UseForeColor = true;
        this.lblLicenseStatus.Location = new System.Drawing.Point(0, 175);
        this.lblLicenseStatus.Name = "lblLicenseStatus";
        this.lblLicenseStatus.Size = new System.Drawing.Size(171, 17);
        this.lblLicenseStatus.TabIndex = 6;
        this.lblLicenseStatus.Text = "Status: Unlicensed / Evaluation";
        // 
        // lblLicensedTo
        // 
        this.lblLicensedTo.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblLicensedTo.Appearance.Options.UseFont = true;
        this.lblLicensedTo.Location = new System.Drawing.Point(0, 200);
        this.lblLicensedTo.Name = "lblLicensedTo";
        this.lblLicensedTo.Size = new System.Drawing.Size(130, 15);
        this.lblLicensedTo.TabIndex = 7;
        this.lblLicensedTo.Text = "Licensed To: Unregistered";
        // 
        // chkEvaluationMode
        // 
        this.chkEvaluationMode.EditValue = true;
        this.chkEvaluationMode.Location = new System.Drawing.Point(0, 240);
        this.chkEvaluationMode.Name = "chkEvaluationMode";
        this.chkEvaluationMode.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkEvaluationMode.Properties.Appearance.Options.UseFont = true;
        this.chkEvaluationMode.Properties.Caption = "Continue in Evaluation / Trial Mode (30-day evaluation period)";
        this.chkEvaluationMode.Size = new System.Drawing.Size(460, 20);
        this.chkEvaluationMode.TabIndex = 8;
        // 
        // panelStep8
        // 
        this.panelStep8.AutoScroll = true;
        this.panelStep8.Controls.Add(this.lblStep8Title);
        this.panelStep8.Controls.Add(this.lblStep8Desc);
        this.panelStep8.Controls.Add(this.memoSummary);
        this.panelStep8.Controls.Add(this.lblFinishNotice);
        this.panelStep8.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelStep8.Location = new System.Drawing.Point(24, 18);
        this.panelStep8.Name = "panelStep8";
        this.panelStep8.Size = new System.Drawing.Size(661, 490);
        this.panelStep8.TabIndex = 7;
        this.panelStep8.Visible = false;
        // 
        // lblStep8Title
        // 
        this.lblStep8Title.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblStep8Title.Appearance.Options.UseFont = true;
        this.lblStep8Title.Location = new System.Drawing.Point(0, 4);
        this.lblStep8Title.Name = "lblStep8Title";
        this.lblStep8Title.Size = new System.Drawing.Size(262, 21);
        this.lblStep8Title.TabIndex = 0;
        this.lblStep8Title.Text = "Review Configuration && Complete";
        // 
        // lblStep8Desc
        // 
        this.lblStep8Desc.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStep8Desc.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
        this.lblStep8Desc.Appearance.Options.UseFont = true;
        this.lblStep8Desc.Appearance.Options.UseForeColor = true;
        this.lblStep8Desc.Location = new System.Drawing.Point(0, 32);
        this.lblStep8Desc.Name = "lblStep8Desc";
        this.lblStep8Desc.Size = new System.Drawing.Size(564, 15);
        this.lblStep8Desc.TabIndex = 1;
        this.lblStep8Desc.Text = "Review your configuration parameters below. Click \'Finish Setup\' to complete comm" +
    "issioning and sign in.";
        // 
        // memoSummary
        // 
        this.memoSummary.Location = new System.Drawing.Point(0, 60);
        this.memoSummary.Name = "memoSummary";
        this.memoSummary.Properties.Appearance.Font = new System.Drawing.Font("Consolas", 9F);
        this.memoSummary.Properties.Appearance.Options.UseFont = true;
        this.memoSummary.Properties.ReadOnly = true;
        this.memoSummary.Properties.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this.memoSummary.Size = new System.Drawing.Size(640, 320);
        this.memoSummary.TabIndex = 2;
        // 
        // lblFinishNotice
        // 
        this.lblFinishNotice.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblFinishNotice.Appearance.ForeColor = System.Drawing.Color.ForestGreen;
        this.lblFinishNotice.Appearance.Options.UseFont = true;
        this.lblFinishNotice.Appearance.Options.UseForeColor = true;
        this.lblFinishNotice.Location = new System.Drawing.Point(0, 395);
        this.lblFinishNotice.Name = "lblFinishNotice";
        this.lblFinishNotice.Size = new System.Drawing.Size(527, 15);
        this.lblFinishNotice.TabIndex = 3;
        this.lblFinishNotice.Text = "All setup parameters are validated. Click \'Finish Setup\' to finalize and launch t" +
    "he Sign-In screen.";
        // 
        // FirstRunWizardForm
        // 
        this.ClientSize = new System.Drawing.Size(1020, 720);
        this.Controls.Add(this.panelContainer);
        this.Controls.Add(this.panelSidebar);
        this.Controls.Add(this.panelBottom);
        this.Controls.Add(this.panelHeader);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = false;
        this.MinimumSize = new System.Drawing.Size(960, 640);
        this.Name = "FirstRunWizardForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Clovent Business Operating System - First-Run Commissioning";
        this.panelHeader.ResumeLayout(false);
        this.panelHeader.PerformLayout();
        this.panelSidebar.ResumeLayout(false);
        this.panelSidebar.PerformLayout();
        this.panelBottom.ResumeLayout(false);
        this.panelBottom.PerformLayout();
        this.panelContainer.ResumeLayout(false);
        this.panelStep1.ResumeLayout(false);
        this.panelStep1.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.grpPrerequisites)).EndInit();
        this.grpPrerequisites.ResumeLayout(false);
        this.grpPrerequisites.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.grpSystemDetection)).EndInit();
        this.grpSystemDetection.ResumeLayout(false);
        this.grpSystemDetection.PerformLayout();
        this.panelStep2.ResumeLayout(false);
        this.panelStep2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.txtServer.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtDatabase.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbAuth.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtUsername.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtPassword.Properties)).EndInit();
        this.panelStep3.ResumeLayout(false);
        this.panelStep3.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.chkCreateDbIfMissing.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkApplyMigrations.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.progressMigrations.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.memoMigrationLog.Properties)).EndInit();
        this.panelStep4.ResumeLayout(false);
        this.panelStep4.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.txtOrgName.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtTaxId.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtCompanyName.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtBranchName.Properties)).EndInit();
        this.panelStep5.ResumeLayout(false);
        this.panelStep5.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.txtAdminUsername.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtAdminFullName.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtAdminEmail.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtAdminPassword.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtAdminConfirmPassword.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.progressPasswordStrength.Properties)).EndInit();
        this.panelStep6.ResumeLayout(false);
        this.panelStep6.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.cmbTimeZone.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbDateFormat.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbTimeFormat.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbCurrency.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtTerminalName.Properties)).EndInit();
        this.panelStep7.ResumeLayout(false);
        this.panelStep7.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.txtHardwareId.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.chkEvaluationMode.Properties)).EndInit();
        this.panelStep8.ResumeLayout(false);
        this.panelStep8.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.memoSummary.Properties)).EndInit();
        this.ResumeLayout(false);

    }

    private System.Windows.Forms.Panel panelHeader;
    private DevExpress.XtraEditors.LabelControl lblWizardTitle;
    private DevExpress.XtraEditors.LabelControl lblWizardSubtitle;
    private System.Windows.Forms.Panel panelSidebar;
    private DevExpress.XtraEditors.LabelControl lblSidebarHeader;
    private DevExpress.XtraEditors.LabelControl lblStep1;
    private DevExpress.XtraEditors.LabelControl lblStep2;
    private DevExpress.XtraEditors.LabelControl lblStep3;
    private DevExpress.XtraEditors.LabelControl lblStep4;
    private DevExpress.XtraEditors.LabelControl lblStep5;
    private DevExpress.XtraEditors.LabelControl lblStep6;
    private DevExpress.XtraEditors.LabelControl lblStep7;
    private DevExpress.XtraEditors.LabelControl lblStep8;
    private System.Windows.Forms.Panel panelBottom;
    private DevExpress.XtraEditors.LabelControl lblStepIndicator;
    private DevExpress.XtraEditors.LabelControl lblFooterStatus;
    private DevExpress.XtraEditors.SimpleButton btnCancel;
    private DevExpress.XtraEditors.SimpleButton btnBack;
    private DevExpress.XtraEditors.SimpleButton btnNext;
    private DevExpress.XtraEditors.SimpleButton btnFinish;
    private System.Windows.Forms.Panel panelContainer;

    // Step 1
    private System.Windows.Forms.Panel panelStep1;
    private DevExpress.XtraEditors.LabelControl lblStep1Title;
    private DevExpress.XtraEditors.LabelControl lblStep1Desc;
    private DevExpress.XtraEditors.GroupControl grpPrerequisites;
    private DevExpress.XtraEditors.LabelControl lblPrereqSql;
    private DevExpress.XtraEditors.LabelControl lblPrereqRuntime;
    private DevExpress.XtraEditors.LabelControl lblPrereqDisplay;
    private DevExpress.XtraEditors.LabelControl lblPrereqAdmin;
    private DevExpress.XtraEditors.GroupControl grpSystemDetection;
    private DevExpress.XtraEditors.LabelControl lblDetectedOs;
    private DevExpress.XtraEditors.LabelControl lblDetectedRuntime;
    private DevExpress.XtraEditors.LabelControl lblDetectedDpi;
    private DevExpress.XtraEditors.LabelControl lblDetectedElevation;

    // Step 2
    private System.Windows.Forms.Panel panelStep2;
    private DevExpress.XtraEditors.LabelControl lblStep2Title;
    private DevExpress.XtraEditors.LabelControl lblStep2Desc;
    private DevExpress.XtraEditors.LabelControl lblServer;
    private DevExpress.XtraEditors.TextEdit txtServer;
    private DevExpress.XtraEditors.LabelControl lblDatabase;
    private DevExpress.XtraEditors.TextEdit txtDatabase;
    private DevExpress.XtraEditors.LabelControl lblAuth;
    private DevExpress.XtraEditors.ComboBoxEdit cmbAuth;
    private DevExpress.XtraEditors.LabelControl lblUsername;
    private DevExpress.XtraEditors.TextEdit txtUsername;
    private DevExpress.XtraEditors.LabelControl lblPassword;
    private DevExpress.XtraEditors.TextEdit txtPassword;
    private DevExpress.XtraEditors.SimpleButton btnTestConnection;
    private DevExpress.XtraEditors.LabelControl lblConnectionStatus;

    // Step 3
    private System.Windows.Forms.Panel panelStep3;
    private DevExpress.XtraEditors.LabelControl lblStep3Title;
    private DevExpress.XtraEditors.LabelControl lblStep3Desc;
    private DevExpress.XtraEditors.CheckEdit chkCreateDbIfMissing;
    private DevExpress.XtraEditors.CheckEdit chkApplyMigrations;
    private DevExpress.XtraEditors.SimpleButton btnApplyMigrations;
    private DevExpress.XtraEditors.ProgressBarControl progressMigrations;
    private DevExpress.XtraEditors.LabelControl lblMigrationStatus;
    private DevExpress.XtraEditors.MemoEdit memoMigrationLog;

    // Step 4
    private System.Windows.Forms.Panel panelStep4;
    private DevExpress.XtraEditors.LabelControl lblStep4Title;
    private DevExpress.XtraEditors.LabelControl lblStep4Desc;
    private DevExpress.XtraEditors.LabelControl lblOrgName;
    private DevExpress.XtraEditors.TextEdit txtOrgName;
    private DevExpress.XtraEditors.LabelControl lblTaxId;
    private DevExpress.XtraEditors.TextEdit txtTaxId;
    private DevExpress.XtraEditors.LabelControl lblCompanyName;
    private DevExpress.XtraEditors.TextEdit txtCompanyName;
    private DevExpress.XtraEditors.LabelControl lblBranchName;
    private DevExpress.XtraEditors.TextEdit txtBranchName;

    // Step 5
    private System.Windows.Forms.Panel panelStep5;
    private DevExpress.XtraEditors.LabelControl lblStep5Title;
    private DevExpress.XtraEditors.LabelControl lblStep5Desc;
    private DevExpress.XtraEditors.LabelControl lblAdminUsername;
    private DevExpress.XtraEditors.TextEdit txtAdminUsername;
    private DevExpress.XtraEditors.LabelControl lblAdminFullName;
    private DevExpress.XtraEditors.TextEdit txtAdminFullName;
    private DevExpress.XtraEditors.LabelControl lblAdminEmail;
    private DevExpress.XtraEditors.TextEdit txtAdminEmail;
    private DevExpress.XtraEditors.LabelControl lblAdminPassword;
    private DevExpress.XtraEditors.TextEdit txtAdminPassword;
    private DevExpress.XtraEditors.LabelControl lblAdminConfirmPassword;
    private DevExpress.XtraEditors.TextEdit txtAdminConfirmPassword;
    private DevExpress.XtraEditors.LabelControl lblPasswordStrength;
    private DevExpress.XtraEditors.ProgressBarControl progressPasswordStrength;
    private DevExpress.XtraEditors.LabelControl lblPasswordPolicy;

    // Step 6
    private System.Windows.Forms.Panel panelStep6;
    private DevExpress.XtraEditors.LabelControl lblStep6Title;
    private DevExpress.XtraEditors.LabelControl lblStep6Desc;
    private DevExpress.XtraEditors.LabelControl lblTimeZone;
    private DevExpress.XtraEditors.ComboBoxEdit cmbTimeZone;
    private DevExpress.XtraEditors.LabelControl lblDateFormat;
    private DevExpress.XtraEditors.ComboBoxEdit cmbDateFormat;
    private DevExpress.XtraEditors.LabelControl lblTimeFormat;
    private DevExpress.XtraEditors.ComboBoxEdit cmbTimeFormat;
    private DevExpress.XtraEditors.LabelControl lblCurrency;
    private DevExpress.XtraEditors.ComboBoxEdit cmbCurrency;
    private DevExpress.XtraEditors.LabelControl lblTerminalName;
    private DevExpress.XtraEditors.TextEdit txtTerminalName;
    private DevExpress.XtraEditors.LabelControl lblSamplePreview;

    // Step 7
    private System.Windows.Forms.Panel panelStep7;
    private DevExpress.XtraEditors.LabelControl lblStep7Title;
    private DevExpress.XtraEditors.LabelControl lblStep7Desc;
    private DevExpress.XtraEditors.LabelControl lblHardwareId;
    private DevExpress.XtraEditors.TextEdit txtHardwareId;
    private DevExpress.XtraEditors.SimpleButton btnCopyHardwareId;
    private DevExpress.XtraEditors.SimpleButton btnImportLicense;
    private DevExpress.XtraEditors.LabelControl lblLicenseStatus;
    private DevExpress.XtraEditors.LabelControl lblLicensedTo;
    private DevExpress.XtraEditors.CheckEdit chkEvaluationMode;

    // Step 8
    private System.Windows.Forms.Panel panelStep8;
    private DevExpress.XtraEditors.LabelControl lblStep8Title;
    private DevExpress.XtraEditors.LabelControl lblStep8Desc;
    private DevExpress.XtraEditors.MemoEdit memoSummary;
    private DevExpress.XtraEditors.LabelControl lblFinishNotice;
}
