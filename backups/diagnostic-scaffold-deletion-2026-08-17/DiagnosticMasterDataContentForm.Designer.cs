namespace Clovent.Desktop.MasterData;

partial class DiagnosticMasterDataContentForm
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Button button1;

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
        this.button1 = new System.Windows.Forms.Button();
        this._contentPanel.SuspendLayout();
        this.SuspendLayout();
        // 
        // button1
        // 
        this.button1.Location = new System.Drawing.Point(50, 50);
        this.button1.Name = "button1";
        this.button1.Size = new System.Drawing.Size(120, 30);
        this.button1.TabIndex = 0;
        this.button1.Text = "Diagnostic Button";
        this.button1.UseVisualStyleBackColor = true;
        // 
        // DiagnosticMasterDataContentForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this._contentPanel.Controls.Add(this.button1);
        this.Name = "DiagnosticMasterDataContentForm";
        this.Text = "DiagnosticMasterDataContentForm";
        this._contentPanel.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
