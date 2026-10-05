namespace Clovent.Desktop.Licensing;

partial class SoftwareRegistrationForm
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
        this.lblHeader = new DevExpress.XtraEditors.LabelControl();
        this.lblStatus = new DevExpress.XtraEditors.LabelControl();
        this.lblProductTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblProductVal = new DevExpress.XtraEditors.LabelControl();
        this.lblVersionTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblVersionVal = new DevExpress.XtraEditors.LabelControl();
        this.lblLicensedToTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblLicensedToVal = new DevExpress.XtraEditors.LabelControl();
        this.lblLicenseIdTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblLicenseIdVal = new DevExpress.XtraEditors.LabelControl();
        this.lblExpiryTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblExpiryVal = new DevExpress.XtraEditors.LabelControl();
        this.lblTerminalsTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblTerminalsVal = new DevExpress.XtraEditors.LabelControl();
        this.lblModulesTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblModulesVal = new DevExpress.XtraEditors.LabelControl();
        this.lblMachineIdTitle = new DevExpress.XtraEditors.LabelControl();
        this.txtMachineId = new DevExpress.XtraEditors.TextEdit();
        this.btnCopyMachineId = new DevExpress.XtraEditors.SimpleButton();
        this.panelBottom = new System.Windows.Forms.Panel();
        this.btnImport = new DevExpress.XtraEditors.SimpleButton();
        this.btnClose = new DevExpress.XtraEditors.SimpleButton();
        ((System.ComponentModel.ISupportInitialize)(this.txtMachineId.Properties)).BeginInit();
        this.panelBottom.SuspendLayout();
        this.SuspendLayout();
        // 
        // lblHeader
        // 
        this.lblHeader.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblHeader.Appearance.Options.UseFont = true;
        this.lblHeader.Location = new System.Drawing.Point(24, 20);
        this.lblHeader.Name = "lblHeader";
        this.lblHeader.Size = new System.Drawing.Size(262, 21);
        this.lblHeader.TabIndex = 0;
        this.lblHeader.Text = "Software Registration && Licensing";
        // 
        // lblStatus
        // 
        this.lblStatus.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
        this.lblStatus.Appearance.ForeColor = System.Drawing.Color.ForestGreen;
        this.lblStatus.Appearance.Options.UseFont = true;
        this.lblStatus.Appearance.Options.UseForeColor = true;
        this.lblStatus.Appearance.Options.UseTextOptions = true;
        this.lblStatus.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        this.lblStatus.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
        this.lblStatus.Location = new System.Drawing.Point(24, 44);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(440, 38);
        this.lblStatus.TabIndex = 1;
        this.lblStatus.Text = "Status: License Valid";
        // 
        // lblProductTitle
        // 
        this.lblProductTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblProductTitle.Appearance.Options.UseFont = true;
        this.lblProductTitle.Location = new System.Drawing.Point(24, 85);
        this.lblProductTitle.Name = "lblProductTitle";
        this.lblProductTitle.Size = new System.Drawing.Size(46, 15);
        this.lblProductTitle.TabIndex = 2;
        this.lblProductTitle.Text = "Product:";
        // 
        // lblProductVal
        // 
        this.lblProductVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblProductVal.Appearance.Options.UseFont = true;
        this.lblProductVal.Location = new System.Drawing.Point(140, 85);
        this.lblProductVal.Name = "lblProductVal";
        this.lblProductVal.Size = new System.Drawing.Size(206, 15);
        this.lblProductVal.TabIndex = 3;
        this.lblProductVal.Text = "Clovent Business Operating System";
        // 
        // lblVersionTitle
        // 
        this.lblVersionTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblVersionTitle.Appearance.Options.UseFont = true;
        this.lblVersionTitle.Location = new System.Drawing.Point(24, 115);
        this.lblVersionTitle.Name = "lblVersionTitle";
        this.lblVersionTitle.Size = new System.Drawing.Size(43, 15);
        this.lblVersionTitle.TabIndex = 4;
        this.lblVersionTitle.Text = "Version:";
        // 
        // lblVersionVal
        // 
        this.lblVersionVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblVersionVal.Appearance.Options.UseFont = true;
        this.lblVersionVal.Location = new System.Drawing.Point(140, 115);
        this.lblVersionVal.Name = "lblVersionVal";
        this.lblVersionVal.Size = new System.Drawing.Size(37, 15);
        this.lblVersionVal.TabIndex = 5;
        this.lblVersionVal.Text = "1.0.0.0";
        // 
        // lblLicensedToTitle
        // 
        this.lblLicensedToTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblLicensedToTitle.Appearance.Options.UseFont = true;
        this.lblLicensedToTitle.Location = new System.Drawing.Point(24, 145);
        this.lblLicensedToTitle.Name = "lblLicensedToTitle";
        this.lblLicensedToTitle.Size = new System.Drawing.Size(68, 15);
        this.lblLicensedToTitle.TabIndex = 6;
        this.lblLicensedToTitle.Text = "Licensed To:";
        // 
        // lblLicensedToVal
        // 
        this.lblLicensedToVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblLicensedToVal.Appearance.Options.UseFont = true;
        this.lblLicensedToVal.Location = new System.Drawing.Point(140, 145);
        this.lblLicensedToVal.Name = "lblLicensedToVal";
        this.lblLicensedToVal.Size = new System.Drawing.Size(126, 15);
        this.lblLicensedToVal.TabIndex = 7;
        this.lblLicensedToVal.Text = "Clovent Commercial User";
        // 
        // lblLicenseIdTitle
        // 
        this.lblLicenseIdTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblLicenseIdTitle.Appearance.Options.UseFont = true;
        this.lblLicenseIdTitle.Location = new System.Drawing.Point(24, 175);
        this.lblLicenseIdTitle.Name = "lblLicenseIdTitle";
        this.lblLicenseIdTitle.Size = new System.Drawing.Size(60, 15);
        this.lblLicenseIdTitle.TabIndex = 8;
        this.lblLicenseIdTitle.Text = "License ID:";
        // 
        // lblLicenseIdVal
        // 
        this.lblLicenseIdVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblLicenseIdVal.Appearance.Options.UseFont = true;
        this.lblLicenseIdVal.Location = new System.Drawing.Point(140, 175);
        this.lblLicenseIdVal.Name = "lblLicenseIdVal";
        this.lblLicenseIdVal.Size = new System.Drawing.Size(220, 15);
        this.lblLicenseIdVal.TabIndex = 9;
        this.lblLicenseIdVal.Text = "00000000-0000-0000-0000-000000000000";
        // 
        // lblExpiryTitle
        // 
        this.lblExpiryTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblExpiryTitle.Appearance.Options.UseFont = true;
        this.lblExpiryTitle.Location = new System.Drawing.Point(24, 205);
        this.lblExpiryTitle.Name = "lblExpiryTitle";
        this.lblExpiryTitle.Size = new System.Drawing.Size(65, 15);
        this.lblExpiryTitle.TabIndex = 10;
        this.lblExpiryTitle.Text = "Expiry Date:";
        // 
        // lblExpiryVal
        // 
        this.lblExpiryVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblExpiryVal.Appearance.Options.UseFont = true;
        this.lblExpiryVal.Location = new System.Drawing.Point(140, 205);
        this.lblExpiryVal.Name = "lblExpiryVal";
        this.lblExpiryVal.Size = new System.Drawing.Size(120, 15);
        this.lblExpiryVal.TabIndex = 11;
        this.lblExpiryVal.Text = "31-Dec-2027 (Perpetual)";
        // 
        // lblTerminalsTitle
        // 
        this.lblTerminalsTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblTerminalsTitle.Appearance.Options.UseFont = true;
        this.lblTerminalsTitle.Location = new System.Drawing.Point(24, 235);
        this.lblTerminalsTitle.Name = "lblTerminalsTitle";
        this.lblTerminalsTitle.Size = new System.Drawing.Size(57, 15);
        this.lblTerminalsTitle.TabIndex = 12;
        this.lblTerminalsTitle.Text = "Terminals:";
        // 
        // lblTerminalsVal
        // 
        this.lblTerminalsVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblTerminalsVal.Appearance.Options.UseFont = true;
        this.lblTerminalsVal.Location = new System.Drawing.Point(140, 235);
        this.lblTerminalsVal.Name = "lblTerminalsVal";
        this.lblTerminalsVal.Size = new System.Drawing.Size(107, 15);
        this.lblTerminalsVal.TabIndex = 13;
        this.lblTerminalsVal.Text = "Unlimited Terminals";
        // 
        // lblModulesTitle
        // 
        this.lblModulesTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblModulesTitle.Appearance.Options.UseFont = true;
        this.lblModulesTitle.Location = new System.Drawing.Point(24, 265);
        this.lblModulesTitle.Name = "lblModulesTitle";
        this.lblModulesTitle.Size = new System.Drawing.Size(51, 15);
        this.lblModulesTitle.TabIndex = 14;
        this.lblModulesTitle.Text = "Modules:";
        // 
        // lblModulesVal
        // 
        this.lblModulesVal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblModulesVal.Appearance.Options.UseFont = true;
        this.lblModulesVal.Location = new System.Drawing.Point(140, 265);
        this.lblModulesVal.Name = "lblModulesVal";
        this.lblModulesVal.Size = new System.Drawing.Size(260, 15);
        this.lblModulesVal.TabIndex = 15;
        this.lblModulesVal.Text = "POS, Back Office, Catalog, Inventory, Restaurant";
        // 
        // lblMachineIdTitle
        // 
        this.lblMachineIdTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblMachineIdTitle.Appearance.Options.UseFont = true;
        this.lblMachineIdTitle.Location = new System.Drawing.Point(24, 305);
        this.lblMachineIdTitle.Name = "lblMachineIdTitle";
        this.lblMachineIdTitle.Size = new System.Drawing.Size(73, 15);
        this.lblMachineIdTitle.TabIndex = 16;
        this.lblMachineIdTitle.Text = "Hardware ID:";
        // 
        // txtMachineId
        // 
        this.txtMachineId.Location = new System.Drawing.Point(140, 302);
        this.txtMachineId.Name = "txtMachineId";
        this.txtMachineId.Properties.Appearance.Font = new System.Drawing.Font("Consolas", 9F);
        this.txtMachineId.Properties.Appearance.Options.UseFont = true;
        this.txtMachineId.Properties.ReadOnly = true;
        this.txtMachineId.Size = new System.Drawing.Size(220, 22);
        this.txtMachineId.TabIndex = 17;
        // 
        // btnCopyMachineId
        // 
        this.btnCopyMachineId.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.btnCopyMachineId.Appearance.Options.UseFont = true;
        this.btnCopyMachineId.Location = new System.Drawing.Point(370, 301);
        this.btnCopyMachineId.Name = "btnCopyMachineId";
        this.btnCopyMachineId.Size = new System.Drawing.Size(95, 24);
        this.btnCopyMachineId.TabIndex = 18;
        this.btnCopyMachineId.Text = "Copy ID";
        // 
        // panelBottom
        // 
        this.panelBottom.BackColor = System.Drawing.Color.Transparent;
        this.panelBottom.Controls.Add(this.btnImport);
        this.panelBottom.Controls.Add(this.btnClose);
        this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.panelBottom.Location = new System.Drawing.Point(0, 356);
        this.panelBottom.Name = "panelBottom";
        this.panelBottom.Padding = new System.Windows.Forms.Padding(24, 12, 24, 16);
        this.panelBottom.Size = new System.Drawing.Size(490, 56);
        this.panelBottom.TabIndex = 19;
        // 
        // btnImport
        // 
        this.btnImport.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnImport.Appearance.Options.UseFont = true;
        this.btnImport.Location = new System.Drawing.Point(24, 12);
        this.btnImport.Name = "btnImport";
        this.btnImport.Size = new System.Drawing.Size(140, 28);
        this.btnImport.TabIndex = 0;
        this.btnImport.Text = "Import License File...";
        // 
        // btnClose
        // 
        this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnClose.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnClose.Appearance.Options.UseFont = true;
        this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.btnClose.Location = new System.Drawing.Point(376, 12);
        this.btnClose.Name = "btnClose";
        this.btnClose.Size = new System.Drawing.Size(90, 28);
        this.btnClose.TabIndex = 1;
        this.btnClose.Text = "Close";
        // 
        // SoftwareRegistrationForm
        // 
        this.AcceptButton = this.btnClose;
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this.btnClose;
        this.ClientSize = new System.Drawing.Size(490, 412);
        this.Controls.Add(this.panelBottom);
        this.Controls.Add(this.btnCopyMachineId);
        this.Controls.Add(this.txtMachineId);
        this.Controls.Add(this.lblMachineIdTitle);
        this.Controls.Add(this.lblModulesVal);
        this.Controls.Add(this.lblModulesTitle);
        this.Controls.Add(this.lblTerminalsVal);
        this.Controls.Add(this.lblTerminalsTitle);
        this.Controls.Add(this.lblExpiryVal);
        this.Controls.Add(this.lblExpiryTitle);
        this.Controls.Add(this.lblLicenseIdVal);
        this.Controls.Add(this.lblLicenseIdTitle);
        this.Controls.Add(this.lblLicensedToVal);
        this.Controls.Add(this.lblLicensedToTitle);
        this.Controls.Add(this.lblVersionVal);
        this.Controls.Add(this.lblVersionTitle);
        this.Controls.Add(this.lblProductVal);
        this.Controls.Add(this.lblProductTitle);
        this.Controls.Add(this.lblStatus);
        this.Controls.Add(this.lblHeader);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "SoftwareRegistrationForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Clovent - Software Registration";
        ((System.ComponentModel.ISupportInitialize)(this.txtMachineId.Properties)).EndInit();
        this.panelBottom.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private DevExpress.XtraEditors.LabelControl lblHeader;
    private DevExpress.XtraEditors.LabelControl lblStatus;
    private DevExpress.XtraEditors.LabelControl lblProductTitle;
    private DevExpress.XtraEditors.LabelControl lblProductVal;
    private DevExpress.XtraEditors.LabelControl lblVersionTitle;
    private DevExpress.XtraEditors.LabelControl lblVersionVal;
    private DevExpress.XtraEditors.LabelControl lblLicensedToTitle;
    private DevExpress.XtraEditors.LabelControl lblLicensedToVal;
    private DevExpress.XtraEditors.LabelControl lblLicenseIdTitle;
    private DevExpress.XtraEditors.LabelControl lblLicenseIdVal;
    private DevExpress.XtraEditors.LabelControl lblExpiryTitle;
    private DevExpress.XtraEditors.LabelControl lblExpiryVal;
    private DevExpress.XtraEditors.LabelControl lblTerminalsTitle;
    private DevExpress.XtraEditors.LabelControl lblTerminalsVal;
    private DevExpress.XtraEditors.LabelControl lblModulesTitle;
    private DevExpress.XtraEditors.LabelControl lblModulesVal;
    private DevExpress.XtraEditors.LabelControl lblMachineIdTitle;
    private DevExpress.XtraEditors.TextEdit txtMachineId;
    private DevExpress.XtraEditors.SimpleButton btnCopyMachineId;
    private System.Windows.Forms.Panel panelBottom;
    private DevExpress.XtraEditors.SimpleButton btnImport;
    private DevExpress.XtraEditors.SimpleButton btnClose;
}
