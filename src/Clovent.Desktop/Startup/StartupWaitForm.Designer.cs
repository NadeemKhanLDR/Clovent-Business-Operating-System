namespace Clovent.Desktop.Startup;

partial class StartupWaitForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>Clean up any resources being used.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.progressPanel = new DevExpress.XtraWaitForm.ProgressPanel();
        this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
        this.tableLayoutPanel.SuspendLayout();
        this.SuspendLayout();
        //
        // progressPanel
        //
        this.progressPanel.Appearance.BackColor = System.Drawing.Color.Transparent;
        this.progressPanel.Appearance.Options.UseBackColor = true;
        this.progressPanel.AppearanceCaption.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold);
        this.progressPanel.AppearanceCaption.Options.UseFont = true;
        this.progressPanel.AppearanceDescription.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular);
        this.progressPanel.AppearanceDescription.Options.UseFont = true;
        this.progressPanel.AutoHeight = true;
        this.progressPanel.AutoWidth = false;
        this.progressPanel.Caption = "Clovent Business Operating System";
        this.progressPanel.Description = "Starting...";
        this.progressPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.progressPanel.ImageHorzOffset = 16;
        this.progressPanel.Location = new System.Drawing.Point(0, 0);
        this.progressPanel.Margin = new System.Windows.Forms.Padding(0);
        this.progressPanel.Name = "progressPanel";
        this.progressPanel.Size = new System.Drawing.Size(460, 60);
        this.progressPanel.TabIndex = 0;
        //
        // tableLayoutPanel
        //
        this.tableLayoutPanel.AutoSize = false;
        this.tableLayoutPanel.BackColor = System.Drawing.Color.Transparent;
        this.tableLayoutPanel.ColumnCount = 1;
        this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutPanel.Controls.Add(this.progressPanel, 0, 0);
        this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutPanel.Name = "tableLayoutPanel";
        this.tableLayoutPanel.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
        this.tableLayoutPanel.RowCount = 1;
        this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutPanel.Size = new System.Drawing.Size(460, 84);
        this.tableLayoutPanel.TabIndex = 1;
        //
        // StartupWaitForm
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
        this.ClientSize = new System.Drawing.Size(460, 84);
        this.Controls.Add(this.tableLayoutPanel);
        this.DoubleBuffered = true;
        this.MinimumSize = new System.Drawing.Size(460, 84);
        this.Name = "StartupWaitForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Starting...";
        this.tableLayoutPanel.ResumeLayout(false);
        this.ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraWaitForm.ProgressPanel progressPanel;
    private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
}
