namespace Clovent.Desktop.Configuration;

partial class DatabaseConnectionDialog
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
        this.lblTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblSubtitle = new DevExpress.XtraEditors.LabelControl();
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
        this.lblStatus = new DevExpress.XtraEditors.LabelControl();
        this.btnTest = new DevExpress.XtraEditors.SimpleButton();
        this.btnSave = new DevExpress.XtraEditors.SimpleButton();
        this.btnElevate = new DevExpress.XtraEditors.SimpleButton();
        this.btnCancel = new DevExpress.XtraEditors.SimpleButton();
        this.panelBottom = new System.Windows.Forms.Panel();
        ((System.ComponentModel.ISupportInitialize)(this.txtServer.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtDatabase.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbAuth.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtUsername.Properties)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtPassword.Properties)).BeginInit();
        this.panelBottom.SuspendLayout();
        this.SuspendLayout();
        // 
        // lblTitle
        // 
        this.lblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblTitle.Appearance.Options.UseFont = true;
        this.lblTitle.Location = new System.Drawing.Point(24, 20);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Size = new System.Drawing.Size(248, 21);
        this.lblTitle.TabIndex = 0;
        this.lblTitle.Text = "Database Connection Settings";
        // 
        // lblSubtitle
        // 
        this.lblSubtitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblSubtitle.Appearance.ForeColor = System.Drawing.Color.Gray;
        this.lblSubtitle.Appearance.Options.UseFont = true;
        this.lblSubtitle.Appearance.Options.UseForeColor = true;
        this.lblSubtitle.Location = new System.Drawing.Point(24, 46);
        this.lblSubtitle.Name = "lblSubtitle";
        this.lblSubtitle.Size = new System.Drawing.Size(378, 15);
        this.lblSubtitle.TabIndex = 1;
        this.lblSubtitle.Text = "Configure the connection to the Clovent Business Operating System database.";
        // 
        // lblServer
        // 
        this.lblServer.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
        this.lblServer.Appearance.Options.UseFont = true;
        this.lblServer.Location = new System.Drawing.Point(24, 80);
        this.lblServer.Name = "lblServer";
        this.lblServer.Size = new System.Drawing.Size(89, 15);
        this.lblServer.TabIndex = 2;
        this.lblServer.Text = "SQL Server / Host:";
        // 
        // txtServer
        // 
        this.txtServer.Location = new System.Drawing.Point(24, 100);
        this.txtServer.Name = "txtServer";
        this.txtServer.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtServer.Properties.Appearance.Options.UseFont = true;
        this.txtServer.Size = new System.Drawing.Size(490, 24);
        this.txtServer.TabIndex = 3;
        // 
        // lblDatabase
        // 
        this.lblDatabase.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
        this.lblDatabase.Appearance.Options.UseFont = true;
        this.lblDatabase.Location = new System.Drawing.Point(24, 134);
        this.lblDatabase.Name = "lblDatabase";
        this.lblDatabase.Size = new System.Drawing.Size(87, 15);
        this.lblDatabase.TabIndex = 4;
        this.lblDatabase.Text = "Database Name:";
        // 
        // txtDatabase
        // 
        this.txtDatabase.Location = new System.Drawing.Point(24, 154);
        this.txtDatabase.Name = "txtDatabase";
        this.txtDatabase.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtDatabase.Properties.Appearance.Options.UseFont = true;
        this.txtDatabase.Size = new System.Drawing.Size(490, 24);
        this.txtDatabase.TabIndex = 5;
        // 
        // lblAuth
        // 
        this.lblAuth.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
        this.lblAuth.Appearance.Options.UseFont = true;
        this.lblAuth.Location = new System.Drawing.Point(24, 188);
        this.lblAuth.Name = "lblAuth";
        this.lblAuth.Size = new System.Drawing.Size(111, 15);
        this.lblAuth.TabIndex = 6;
        this.lblAuth.Text = "Authentication Type:";
        // 
        // cmbAuth
        // 
        this.cmbAuth.Location = new System.Drawing.Point(24, 208);
        this.cmbAuth.Name = "cmbAuth";
        this.cmbAuth.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.cmbAuth.Properties.Appearance.Options.UseFont = true;
        this.cmbAuth.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
        this.cmbAuth.Properties.Items.AddRange(new object[] {
            "Windows Authentication (Integrated)",
            "SQL Server Authentication (Username / Password)"});
        this.cmbAuth.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        this.cmbAuth.Size = new System.Drawing.Size(490, 24);
        this.cmbAuth.TabIndex = 7;
        // 
        // lblUsername
        // 
        this.lblUsername.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
        this.lblUsername.Appearance.Options.UseFont = true;
        this.lblUsername.Location = new System.Drawing.Point(24, 242);
        this.lblUsername.Name = "lblUsername";
        this.lblUsername.Size = new System.Drawing.Size(43, 15);
        this.lblUsername.TabIndex = 8;
        this.lblUsername.Text = "User ID:";
        // 
        // txtUsername
        // 
        this.txtUsername.Location = new System.Drawing.Point(24, 262);
        this.txtUsername.Name = "txtUsername";
        this.txtUsername.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtUsername.Properties.Appearance.Options.UseFont = true;
        this.txtUsername.Size = new System.Drawing.Size(490, 24);
        this.txtUsername.TabIndex = 9;
        // 
        // lblPassword
        // 
        this.lblPassword.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
        this.lblPassword.Appearance.Options.UseFont = true;
        this.lblPassword.Location = new System.Drawing.Point(24, 296);
        this.lblPassword.Name = "lblPassword";
        this.lblPassword.Size = new System.Drawing.Size(53, 15);
        this.lblPassword.TabIndex = 10;
        this.lblPassword.Text = "Password:";
        // 
        // txtPassword
        // 
        this.txtPassword.Location = new System.Drawing.Point(24, 316);
        this.txtPassword.Name = "txtPassword";
        this.txtPassword.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtPassword.Properties.Appearance.Options.UseFont = true;
        this.txtPassword.Properties.UseSystemPasswordChar = true;
        this.txtPassword.Size = new System.Drawing.Size(490, 24);
        this.txtPassword.TabIndex = 11;
        // 
        // lblStatus
        // 
        this.lblStatus.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.lblStatus.Appearance.Options.UseFont = true;
        this.lblStatus.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        this.lblStatus.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
        this.lblStatus.Location = new System.Drawing.Point(24, 348);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(490, 36);
        this.lblStatus.TabIndex = 12;
        // 
        // panelBottom
        // 
        this.panelBottom.BackColor = System.Drawing.Color.Transparent;
        this.panelBottom.Controls.Add(this.btnTest);
        this.panelBottom.Controls.Add(this.btnSave);
        this.panelBottom.Controls.Add(this.btnElevate);
        this.panelBottom.Controls.Add(this.btnCancel);
        this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.panelBottom.Location = new System.Drawing.Point(0, 388);
        this.panelBottom.Name = "panelBottom";
        this.panelBottom.Padding = new System.Windows.Forms.Padding(24, 12, 24, 16);
        this.panelBottom.Size = new System.Drawing.Size(540, 56);
        this.panelBottom.TabIndex = 13;
        // 
        // btnTest
        // 
        this.btnTest.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnTest.Appearance.Options.UseFont = true;
        this.btnTest.Location = new System.Drawing.Point(24, 12);
        this.btnTest.Name = "btnTest";
        this.btnTest.Size = new System.Drawing.Size(120, 28);
        this.btnTest.TabIndex = 0;
        this.btnTest.Text = "Test Connection";
        // 
        // btnSave
        // 
        this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnSave.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnSave.Appearance.Options.UseFont = true;
        this.btnSave.Location = new System.Drawing.Point(296, 12);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(110, 28);
        this.btnSave.TabIndex = 1;
        this.btnSave.Text = "Save && Apply";
        // 
        // btnElevate
        // 
        this.btnElevate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnElevate.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnElevate.Appearance.Options.UseFont = true;
        this.btnElevate.Location = new System.Drawing.Point(296, 12);
        this.btnElevate.Name = "btnElevate";
        this.btnElevate.Size = new System.Drawing.Size(110, 28);
        this.btnElevate.TabIndex = 2;
        this.btnElevate.Text = "Elevate (UAC)...";
        this.btnElevate.Visible = false;
        // 
        // btnCancel
        // 
        this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnCancel.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnCancel.Appearance.Options.UseFont = true;
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(414, 12);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(100, 28);
        this.btnCancel.TabIndex = 2;
        this.btnCancel.Text = "Cancel";
        // 
        // DatabaseConnectionDialog
        // 
        this.AcceptButton = this.btnSave;
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(540, 444);
        this.Controls.Add(this.panelBottom);
        this.Controls.Add(this.lblStatus);
        this.Controls.Add(this.txtPassword);
        this.Controls.Add(this.lblPassword);
        this.Controls.Add(this.txtUsername);
        this.Controls.Add(this.lblUsername);
        this.Controls.Add(this.cmbAuth);
        this.Controls.Add(this.lblAuth);
        this.Controls.Add(this.txtDatabase);
        this.Controls.Add(this.lblDatabase);
        this.Controls.Add(this.txtServer);
        this.Controls.Add(this.lblServer);
        this.Controls.Add(this.lblSubtitle);
        this.Controls.Add(this.lblTitle);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "DatabaseConnectionDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Clovent - Database Connection Settings";
        ((System.ComponentModel.ISupportInitialize)(this.txtServer.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtDatabase.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.cmbAuth.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtUsername.Properties)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.txtPassword.Properties)).EndInit();
        this.panelBottom.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private DevExpress.XtraEditors.LabelControl lblTitle;
    private DevExpress.XtraEditors.LabelControl lblSubtitle;
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
    private DevExpress.XtraEditors.LabelControl lblStatus;
    private System.Windows.Forms.Panel panelBottom;
    private DevExpress.XtraEditors.SimpleButton btnTest;
    private DevExpress.XtraEditors.SimpleButton btnSave;
    private DevExpress.XtraEditors.SimpleButton btnElevate;
    private DevExpress.XtraEditors.SimpleButton btnCancel;
}
