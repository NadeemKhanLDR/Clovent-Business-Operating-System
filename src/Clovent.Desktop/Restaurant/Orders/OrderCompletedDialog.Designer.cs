namespace Clovent.Desktop.Restaurant.Orders;

partial class OrderCompletedDialog
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
        this.lblHeader = new DevExpress.XtraEditors.LabelControl();
        this.lblOrderNumber = new DevExpress.XtraEditors.LabelControl();
        this.lblInvoiceNumber = new DevExpress.XtraEditors.LabelControl();
        this.grpSummary = new DevExpress.XtraEditors.GroupControl();
        this.lblGrandTotal = new DevExpress.XtraEditors.LabelControl();
        this.lblTendered = new DevExpress.XtraEditors.LabelControl();
        this.lblChangeDue = new DevExpress.XtraEditors.LabelControl();
        this.grpPrintStatus = new DevExpress.XtraEditors.GroupControl();
        this.lblPrintStatus = new DevExpress.XtraEditors.LabelControl();
        this.lblSpoolerNotice = new DevExpress.XtraEditors.LabelControl();
        this.lblReprintInfo = new DevExpress.XtraEditors.LabelControl();
        this.panelBottom = new System.Windows.Forms.Panel();
        this.btnPrintReceipt = new DevExpress.XtraEditors.SimpleButton();
        this.btnReprintReceipt = new DevExpress.XtraEditors.SimpleButton();
        this.btnDone = new DevExpress.XtraEditors.SimpleButton();
        ((System.ComponentModel.ISupportInitialize)(this.grpSummary)).BeginInit();
        this.grpSummary.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.grpPrintStatus)).BeginInit();
        this.grpPrintStatus.SuspendLayout();
        this.panelBottom.SuspendLayout();
        this.SuspendLayout();
        // 
        // lblHeader
        // 
        this.lblHeader.Appearance.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
        this.lblHeader.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(21)))), ((int)(((byte)(128)))), ((int)(((byte)(61)))));
        this.lblHeader.Appearance.Options.UseFont = true;
        this.lblHeader.Appearance.Options.UseForeColor = true;
        this.lblHeader.Location = new System.Drawing.Point(20, 16);
        this.lblHeader.Name = "lblHeader";
        this.lblHeader.Size = new System.Drawing.Size(262, 25);
        this.lblHeader.TabIndex = 0;
        this.lblHeader.Text = "✓ Order Completed";
        // 
        // lblOrderNumber
        // 
        this.lblOrderNumber.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblOrderNumber.Appearance.Options.UseFont = true;
        this.lblOrderNumber.Location = new System.Drawing.Point(20, 48);
        this.lblOrderNumber.Name = "lblOrderNumber";
        this.lblOrderNumber.Size = new System.Drawing.Size(126, 17);
        this.lblOrderNumber.TabIndex = 1;
        this.lblOrderNumber.Text = "Order: #ORD-0000";
        // 
        // lblInvoiceNumber
        // 
        this.lblInvoiceNumber.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.lblInvoiceNumber.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblInvoiceNumber.Appearance.ForeColor = System.Drawing.Color.Gray;
        this.lblInvoiceNumber.Appearance.Options.UseFont = true;
        this.lblInvoiceNumber.Appearance.Options.UseForeColor = true;
        this.lblInvoiceNumber.Appearance.Options.UseTextOptions = true;
        this.lblInvoiceNumber.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        this.lblInvoiceNumber.Location = new System.Drawing.Point(340, 50);
        this.lblInvoiceNumber.Name = "lblInvoiceNumber";
        this.lblInvoiceNumber.Size = new System.Drawing.Size(200, 15);
        this.lblInvoiceNumber.TabIndex = 2;
        this.lblInvoiceNumber.Text = "Sale / Invoice #: --";
        // 
        // grpSummary
        // 
        this.grpSummary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
        this.grpSummary.Controls.Add(this.lblGrandTotal);
        this.grpSummary.Controls.Add(this.lblTendered);
        this.grpSummary.Controls.Add(this.lblChangeDue);
        this.grpSummary.Location = new System.Drawing.Point(20, 78);
        this.grpSummary.Name = "grpSummary";
        this.grpSummary.Size = new System.Drawing.Size(520, 115);
        this.grpSummary.TabIndex = 3;
        this.grpSummary.Text = "Payment Breakdown";
        // 
        // lblGrandTotal
        // 
        this.lblGrandTotal.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F);
        this.lblGrandTotal.Appearance.Options.UseFont = true;
        this.lblGrandTotal.Location = new System.Drawing.Point(16, 32);
        this.lblGrandTotal.Name = "lblGrandTotal";
        this.lblGrandTotal.Size = new System.Drawing.Size(117, 17);
        this.lblGrandTotal.TabIndex = 0;
        this.lblGrandTotal.Text = "Grand Total: $0.00";
        // 
        // lblTendered
        // 
        this.lblTendered.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F);
        this.lblTendered.Appearance.Options.UseFont = true;
        this.lblTendered.Location = new System.Drawing.Point(16, 56);
        this.lblTendered.Name = "lblTendered";
        this.lblTendered.Size = new System.Drawing.Size(130, 17);
        this.lblTendered.TabIndex = 1;
        this.lblTendered.Text = "Total Tendered: $0.00";
        // 
        // lblChangeDue
        // 
        this.lblChangeDue.Appearance.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
        this.lblChangeDue.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
        this.lblChangeDue.Appearance.Options.UseFont = true;
        this.lblChangeDue.Appearance.Options.UseForeColor = true;
        this.lblChangeDue.Location = new System.Drawing.Point(16, 80);
        this.lblChangeDue.Name = "lblChangeDue";
        this.lblChangeDue.Size = new System.Drawing.Size(157, 23);
        this.lblChangeDue.TabIndex = 2;
        this.lblChangeDue.Text = "Change Due: $0.00";
        // 
        // grpPrintStatus
        // 
        this.grpPrintStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
        this.grpPrintStatus.Controls.Add(this.lblReprintInfo);
        this.grpPrintStatus.Controls.Add(this.lblSpoolerNotice);
        this.grpPrintStatus.Controls.Add(this.lblPrintStatus);
        this.grpPrintStatus.Location = new System.Drawing.Point(20, 204);
        this.grpPrintStatus.Name = "grpPrintStatus";
        this.grpPrintStatus.Size = new System.Drawing.Size(520, 125);
        this.grpPrintStatus.TabIndex = 4;
        this.grpPrintStatus.Text = "Receipt Printing & Spooler Reliability";
        // 
        // lblPrintStatus
        // 
        this.lblPrintStatus.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblPrintStatus.Appearance.Options.UseFont = true;
        this.lblPrintStatus.Location = new System.Drawing.Point(16, 30);
        this.lblPrintStatus.Name = "lblPrintStatus";
        this.lblPrintStatus.Size = new System.Drawing.Size(350, 15);
        this.lblPrintStatus.TabIndex = 0;
        this.lblPrintStatus.Text = "Status: Configured receipt copies dispatched to Windows Spooler.";
        // 
        // lblSpoolerNotice
        // 
        this.lblSpoolerNotice.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.25F);
        this.lblSpoolerNotice.Appearance.ForeColor = System.Drawing.Color.Gray;
        this.lblSpoolerNotice.Appearance.Options.UseFont = true;
        this.lblSpoolerNotice.Appearance.Options.UseForeColor = true;
        this.lblSpoolerNotice.Location = new System.Drawing.Point(16, 54);
        this.lblSpoolerNotice.Name = "lblSpoolerNotice";
        this.lblSpoolerNotice.Size = new System.Drawing.Size(465, 26);
        this.lblSpoolerNotice.TabIndex = 1;
        this.lblSpoolerNotice.Text = "Note: Spooler submission confirms background job acceptance. Physical delivery depends on\r\nprinter paper and hardware condition. Printer failure does not reverse committed sales.";
        // 
        // lblReprintInfo
        // 
        this.lblReprintInfo.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic);
        this.lblReprintInfo.Appearance.ForeColor = System.Drawing.Color.DimGray;
        this.lblReprintInfo.Appearance.Options.UseFont = true;
        this.lblReprintInfo.Appearance.Options.UseForeColor = true;
        this.lblReprintInfo.Location = new System.Drawing.Point(16, 92);
        this.lblReprintInfo.Name = "lblReprintInfo";
        this.lblReprintInfo.Size = new System.Drawing.Size(129, 13);
        this.lblReprintInfo.TabIndex = 2;
        this.lblReprintInfo.Text = "Historical Reprints: 0 copies";
        // 
        // panelBottom
        // 
        this.panelBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(247)))));
        this.panelBottom.Controls.Add(this.btnDone);
        this.panelBottom.Controls.Add(this.btnReprintReceipt);
        this.panelBottom.Controls.Add(this.btnPrintReceipt);
        this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.panelBottom.Location = new System.Drawing.Point(0, 345);
        this.panelBottom.Name = "panelBottom";
        this.panelBottom.Size = new System.Drawing.Size(560, 55);
        this.panelBottom.TabIndex = 5;
        // 
        // btnPrintReceipt
        // 
        this.btnPrintReceipt.Location = new System.Drawing.Point(20, 12);
        this.btnPrintReceipt.Name = "btnPrintReceipt";
        this.btnPrintReceipt.Size = new System.Drawing.Size(130, 32);
        this.btnPrintReceipt.TabIndex = 0;
        this.btnPrintReceipt.Text = "Print Receipt";
        // 
        // btnReprintReceipt
        // 
        this.btnReprintReceipt.Location = new System.Drawing.Point(160, 12);
        this.btnReprintReceipt.Name = "btnReprintReceipt";
        this.btnReprintReceipt.Size = new System.Drawing.Size(140, 32);
        this.btnReprintReceipt.TabIndex = 1;
        this.btnReprintReceipt.Text = "Reprint Receipt...";
        // 
        // btnDone
        // 
        this.btnDone.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnDone.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnDone.Appearance.Options.UseFont = true;
        this.btnDone.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.btnDone.Location = new System.Drawing.Point(400, 12);
        this.btnDone.Name = "btnDone";
        this.btnDone.Size = new System.Drawing.Size(140, 32);
        this.btnDone.TabIndex = 2;
        this.btnDone.Text = "Next Order (Enter)";
        // 
        // OrderCompletedDialog
        // 
        this.AcceptButton = this.btnDone;
        this.ClientSize = new System.Drawing.Size(560, 400);
        this.Controls.Add(this.panelBottom);
        this.Controls.Add(this.grpPrintStatus);
        this.Controls.Add(this.grpSummary);
        this.Controls.Add(this.lblInvoiceNumber);
        this.Controls.Add(this.lblOrderNumber);
        this.Controls.Add(this.lblHeader);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "OrderCompletedDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Order Completed";
        ((System.ComponentModel.ISupportInitialize)(this.grpSummary)).EndInit();
        this.grpSummary.ResumeLayout(false);
        this.grpSummary.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.grpPrintStatus)).EndInit();
        this.grpPrintStatus.ResumeLayout(false);
        this.grpPrintStatus.PerformLayout();
        this.panelBottom.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private DevExpress.XtraEditors.LabelControl lblHeader;
    private DevExpress.XtraEditors.LabelControl lblOrderNumber;
    private DevExpress.XtraEditors.LabelControl lblInvoiceNumber;
    private DevExpress.XtraEditors.GroupControl grpSummary;
    private DevExpress.XtraEditors.LabelControl lblGrandTotal;
    private DevExpress.XtraEditors.LabelControl lblTendered;
    private DevExpress.XtraEditors.LabelControl lblChangeDue;
    private DevExpress.XtraEditors.GroupControl grpPrintStatus;
    private DevExpress.XtraEditors.LabelControl lblPrintStatus;
    private DevExpress.XtraEditors.LabelControl lblSpoolerNotice;
    private DevExpress.XtraEditors.LabelControl lblReprintInfo;
    private System.Windows.Forms.Panel panelBottom;
    private DevExpress.XtraEditors.SimpleButton btnPrintReceipt;
    private DevExpress.XtraEditors.SimpleButton btnReprintReceipt;
    private DevExpress.XtraEditors.SimpleButton btnDone;
}
