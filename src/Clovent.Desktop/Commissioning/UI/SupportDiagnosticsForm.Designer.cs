namespace Clovent.Desktop.Commissioning.UI;

partial class SupportDiagnosticsForm
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
        this.lblTitle = new DevExpress.XtraEditors.LabelControl();
        this.lblSubtitle = new DevExpress.XtraEditors.LabelControl();
        this.panelBottom = new System.Windows.Forms.Panel();
        this.lblCopiedNotice = new DevExpress.XtraEditors.LabelControl();
        this.btnCopy = new DevExpress.XtraEditors.SimpleButton();
        this.btnRefresh = new DevExpress.XtraEditors.SimpleButton();
        this.btnClose = new DevExpress.XtraEditors.SimpleButton();
        this.panelContent = new System.Windows.Forms.Panel();
        this.memoDiagnostics = new DevExpress.XtraEditors.MemoEdit();
        this.panelHeader.SuspendLayout();
        this.panelBottom.SuspendLayout();
        this.panelContent.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.memoDiagnostics.Properties)).BeginInit();
        this.SuspendLayout();
        // 
        // panelHeader
        // 
        this.panelHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
        this.panelHeader.Controls.Add(this.lblTitle);
        this.panelHeader.Controls.Add(this.lblSubtitle);
        this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
        this.panelHeader.Location = new System.Drawing.Point(0, 0);
        this.panelHeader.Name = "panelHeader";
        this.panelHeader.Padding = new System.Windows.Forms.Padding(20, 14, 20, 10);
        this.panelHeader.Size = new System.Drawing.Size(784, 68);
        this.panelHeader.TabIndex = 0;
        // 
        // lblTitle
        // 
        this.lblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12.5F, System.Drawing.FontStyle.Bold);
        this.lblTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
        this.lblTitle.Appearance.Options.UseFont = true;
        this.lblTitle.Appearance.Options.UseForeColor = true;
        this.lblTitle.Location = new System.Drawing.Point(20, 12);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Size = new System.Drawing.Size(260, 23);
        this.lblTitle.TabIndex = 0;
        this.lblTitle.Text = "System && Support Diagnostics";
        // 
        // lblSubtitle
        // 
        this.lblSubtitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblSubtitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
        this.lblSubtitle.Appearance.Options.UseFont = true;
        this.lblSubtitle.Appearance.Options.UseForeColor = true;
        this.lblSubtitle.Location = new System.Drawing.Point(20, 39);
        this.lblSubtitle.Name = "lblSubtitle";
        this.lblSubtitle.Size = new System.Drawing.Size(437, 15);
        this.lblSubtitle.TabIndex = 1;
        this.lblSubtitle.Text = "Diagnostic environment and configuration details. Non-sensitive information only.";
        // 
        // panelBottom
        // 
        this.panelBottom.Controls.Add(this.lblCopiedNotice);
        this.panelBottom.Controls.Add(this.btnCopy);
        this.panelBottom.Controls.Add(this.btnRefresh);
        this.panelBottom.Controls.Add(this.btnClose);
        this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.panelBottom.Location = new System.Drawing.Point(0, 506);
        this.panelBottom.Name = "panelBottom";
        this.panelBottom.Padding = new System.Windows.Forms.Padding(18, 12, 18, 12);
        this.panelBottom.Size = new System.Drawing.Size(784, 55);
        this.panelBottom.TabIndex = 2;
        // 
        // lblCopiedNotice
        // 
        this.lblCopiedNotice.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblCopiedNotice.Appearance.ForeColor = System.Drawing.Color.ForestGreen;
        this.lblCopiedNotice.Appearance.Options.UseFont = true;
        this.lblCopiedNotice.Appearance.Options.UseForeColor = true;
        this.lblCopiedNotice.Location = new System.Drawing.Point(260, 20);
        this.lblCopiedNotice.Name = "lblCopiedNotice";
        this.lblCopiedNotice.Size = new System.Drawing.Size(183, 15);
        this.lblCopiedNotice.TabIndex = 2;
        this.lblCopiedNotice.Text = "Diagnostics copied to clipboard!";
        this.lblCopiedNotice.Visible = false;
        // 
        // btnCopy
        // 
        this.btnCopy.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnCopy.Appearance.Options.UseFont = true;
        this.btnCopy.Location = new System.Drawing.Point(18, 12);
        this.btnCopy.Name = "btnCopy";
        this.btnCopy.Size = new System.Drawing.Size(135, 30);
        this.btnCopy.TabIndex = 0;
        this.btnCopy.Text = "Copy Diagnostics";
        // 
        // btnRefresh
        // 
        this.btnRefresh.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnRefresh.Appearance.Options.UseFont = true;
        this.btnRefresh.Location = new System.Drawing.Point(160, 12);
        this.btnRefresh.Name = "btnRefresh";
        this.btnRefresh.Size = new System.Drawing.Size(88, 30);
        this.btnRefresh.TabIndex = 1;
        this.btnRefresh.Text = "Refresh";
        // 
        // btnClose
        // 
        this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnClose.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnClose.Appearance.Options.UseFont = true;
        this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.btnClose.Location = new System.Drawing.Point(678, 12);
        this.btnClose.Name = "btnClose";
        this.btnClose.Size = new System.Drawing.Size(88, 30);
        this.btnClose.TabIndex = 3;
        this.btnClose.Text = "Close";
        // 
        // panelContent
        // 
        this.panelContent.Controls.Add(this.memoDiagnostics);
        this.panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelContent.Location = new System.Drawing.Point(0, 68);
        this.panelContent.Name = "panelContent";
        this.panelContent.Padding = new System.Windows.Forms.Padding(18, 12, 18, 6);
        this.panelContent.Size = new System.Drawing.Size(784, 438);
        this.panelContent.TabIndex = 1;
        // 
        // memoDiagnostics
        // 
        this.memoDiagnostics.Dock = System.Windows.Forms.DockStyle.Fill;
        this.memoDiagnostics.Location = new System.Drawing.Point(18, 12);
        this.memoDiagnostics.Name = "memoDiagnostics";
        this.memoDiagnostics.Properties.Appearance.Font = new System.Drawing.Font("Consolas", 9.25F);
        this.memoDiagnostics.Properties.Appearance.Options.UseFont = true;
        this.memoDiagnostics.Properties.ReadOnly = true;
        this.memoDiagnostics.Properties.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this.memoDiagnostics.Properties.WordWrap = false;
        this.memoDiagnostics.Size = new System.Drawing.Size(748, 420);
        this.memoDiagnostics.TabIndex = 0;
        // 
        // SupportDiagnosticsForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(784, 561);
        this.Controls.Add(this.panelContent);
        this.Controls.Add(this.panelBottom);
        this.Controls.Add(this.panelHeader);
        this.MinimumSize = new System.Drawing.Size(700, 480);
        this.Name = "SupportDiagnosticsForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "System && Support Diagnostics";
        this.panelHeader.ResumeLayout(false);
        this.panelHeader.PerformLayout();
        this.panelBottom.ResumeLayout(false);
        this.panelBottom.PerformLayout();
        this.panelContent.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.memoDiagnostics.Properties)).EndInit();
        this.ResumeLayout(false);

    }

    private System.Windows.Forms.Panel panelHeader;
    private DevExpress.XtraEditors.LabelControl lblTitle;
    private DevExpress.XtraEditors.LabelControl lblSubtitle;
    private System.Windows.Forms.Panel panelBottom;
    private DevExpress.XtraEditors.LabelControl lblCopiedNotice;
    private DevExpress.XtraEditors.SimpleButton btnCopy;
    private DevExpress.XtraEditors.SimpleButton btnRefresh;
    private DevExpress.XtraEditors.SimpleButton btnClose;
    private System.Windows.Forms.Panel panelContent;
    private DevExpress.XtraEditors.MemoEdit memoDiagnostics;
}
