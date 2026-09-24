using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Customers;

partial class CustomerPaymentForm
{
    private System.ComponentModel.IContainer components = null;

    private TextEdit _txtCustomer;
    private TextEdit _txtOutstanding;
    private SpinEdit _spinAmount;
    private ComboBoxEdit _comboPaymentMethod;
    private TextEdit _txtReference;
    private MemoEdit _txtNotes;
    private SimpleButton _btnCancel;
    private SimpleButton _btnSubmit;

    // Layout Panels and Static Labels
    private TableLayoutPanel root;
    private TableLayoutPanel fieldTable;
    private FlowLayoutPanel btnPanel;
    private LabelControl lblCustomer;
    private LabelControl lblOutstanding;
    private LabelControl lblAmount;
    private LabelControl lblPaymentMethod;
    private LabelControl lblReference;
    private LabelControl lblNotes;

    /// <summary>Clean up any resources being used.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _txtCustomer = new TextEdit();
        _txtOutstanding = new TextEdit();
        _spinAmount = new SpinEdit();
        _comboPaymentMethod = new ComboBoxEdit();
        _txtReference = new TextEdit();
        _txtNotes = new MemoEdit();
        _btnCancel = new SimpleButton();
        _btnSubmit = new SimpleButton();
        root = new TableLayoutPanel();
        fieldTable = new TableLayoutPanel();
        lblCustomer = new LabelControl();
        lblOutstanding = new LabelControl();
        lblAmount = new LabelControl();
        lblPaymentMethod = new LabelControl();
        lblReference = new LabelControl();
        lblNotes = new LabelControl();
        btnPanel = new FlowLayoutPanel();
        ((System.ComponentModel.ISupportInitialize)_txtCustomer.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtOutstanding.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_spinAmount.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_comboPaymentMethod.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtReference.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtNotes.Properties).BeginInit();
        root.SuspendLayout();
        fieldTable.SuspendLayout();
        btnPanel.SuspendLayout();
        SuspendLayout();
        // 
        // _txtCustomer
        // 
        _txtCustomer.Dock = DockStyle.Fill;
        _txtCustomer.Location = new Point(133, 3);
        _txtCustomer.Name = "_txtCustomer";
        _txtCustomer.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtCustomer.Properties.Appearance.Options.UseFont = true;
        _txtCustomer.Properties.ReadOnly = true;
        _txtCustomer.Size = new Size(284, 24);
        _txtCustomer.TabIndex = 1;
        // 
        // _txtOutstanding
        // 
        _txtOutstanding.Dock = DockStyle.Fill;
        _txtOutstanding.Location = new Point(133, 39);
        _txtOutstanding.Name = "_txtOutstanding";
        _txtOutstanding.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtOutstanding.Properties.Appearance.Options.UseFont = true;
        _txtOutstanding.Properties.AppearanceReadOnly.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _txtOutstanding.Properties.AppearanceReadOnly.Options.UseFont = true;
        _txtOutstanding.Properties.ReadOnly = true;
        _txtOutstanding.Size = new Size(284, 24);
        _txtOutstanding.TabIndex = 3;
        // 
        // _spinAmount
        // 
        _spinAmount.Dock = DockStyle.Fill;
        _spinAmount.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
        _spinAmount.Location = new Point(133, 75);
        _spinAmount.Name = "_spinAmount";
        _spinAmount.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _spinAmount.Properties.Appearance.Options.UseFont = true;
        _spinAmount.Properties.Mask.EditMask = "n2";
        _spinAmount.Properties.Mask.UseMaskAsDisplayFormat = true;
        _spinAmount.Properties.MaxValue = new decimal(new int[] { 99999999, 0, 0, 0 });
        _spinAmount.Properties.MinValue = new decimal(new int[] { 1, 0, 0, 131072 });
        _spinAmount.Size = new Size(284, 24);
        _spinAmount.TabIndex = 5;
        // 
        // _comboPaymentMethod
        // 
        _comboPaymentMethod.Dock = DockStyle.Fill;
        _comboPaymentMethod.Location = new Point(133, 111);
        _comboPaymentMethod.Name = "_comboPaymentMethod";
        _comboPaymentMethod.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _comboPaymentMethod.Properties.Appearance.Options.UseFont = true;
        _comboPaymentMethod.Properties.Items.AddRange(new object[] { "Cash", "Credit", "Credit Card" });
        _comboPaymentMethod.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _comboPaymentMethod.Size = new Size(284, 24);
        _comboPaymentMethod.TabIndex = 7;
        // 
        // _txtReference
        // 
        _txtReference.Dock = DockStyle.Fill;
        _txtReference.Location = new Point(133, 147);
        _txtReference.Name = "_txtReference";
        _txtReference.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtReference.Properties.Appearance.Options.UseFont = true;
        _txtReference.Size = new Size(284, 24);
        _txtReference.TabIndex = 9;
        // 
        // _txtNotes
        // 
        _txtNotes.Dock = DockStyle.Fill;
        _txtNotes.Location = new Point(133, 183);
        _txtNotes.Name = "_txtNotes";
        _txtNotes.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtNotes.Properties.Appearance.Options.UseFont = true;
        _txtNotes.Size = new Size(284, 84);
        _txtNotes.TabIndex = 11;
        // 
        // _btnCancel
        // 
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Location = new Point(322, 7);
        _btnCancel.MinimumSize = new Size(95, 34);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new Size(95, 34);
        _btnCancel.TabIndex = 0;
        _btnCancel.Text = "Cancel";
        // 
        // _btnSubmit
        // 
        _btnSubmit.Location = new Point(186, 7);
        _btnSubmit.MinimumSize = new Size(130, 34);
        _btnSubmit.Name = "_btnSubmit";
        _btnSubmit.Size = new Size(130, 34);
        _btnSubmit.TabIndex = 1;
        _btnSubmit.Text = "Receive Payment";
        _btnSubmit.Click += BtnSubmit_Click;
        // 
        // root
        // 
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        root.Controls.Add(fieldTable, 0, 0);
        root.Controls.Add(btnPanel, 0, 1);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Name = "root";
        root.Padding = new Padding(16);
        root.RowCount = 2;
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        root.Size = new Size(458, 358);
        root.TabIndex = 0;
        // 
        // fieldTable
        // 
        fieldTable.ColumnCount = 2;
        fieldTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        fieldTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        fieldTable.Controls.Add(lblCustomer, 0, 0);
        fieldTable.Controls.Add(_txtCustomer, 1, 0);
        fieldTable.Controls.Add(lblOutstanding, 0, 1);
        fieldTable.Controls.Add(_txtOutstanding, 1, 1);
        fieldTable.Controls.Add(lblAmount, 0, 2);
        fieldTable.Controls.Add(_spinAmount, 1, 2);
        fieldTable.Controls.Add(lblPaymentMethod, 0, 3);
        fieldTable.Controls.Add(_comboPaymentMethod, 1, 3);
        fieldTable.Controls.Add(lblReference, 0, 4);
        fieldTable.Controls.Add(_txtReference, 1, 4);
        fieldTable.Controls.Add(lblNotes, 0, 5);
        fieldTable.Controls.Add(_txtNotes, 1, 5);
        fieldTable.Dock = DockStyle.Fill;
        fieldTable.Location = new Point(19, 19);
        fieldTable.Name = "fieldTable";
        fieldTable.RowCount = 6;
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
        fieldTable.Size = new Size(420, 270);
        fieldTable.TabIndex = 0;
        // 
        // lblCustomer
        // 
        lblCustomer.Appearance.Font = new Font("Segoe UI", 9.5F);
        lblCustomer.Appearance.Options.UseFont = true;
        lblCustomer.Appearance.Options.UseTextOptions = true;
        lblCustomer.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblCustomer.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblCustomer.Dock = DockStyle.Fill;
        lblCustomer.Location = new Point(3, 3);
        lblCustomer.Name = "lblCustomer";
        lblCustomer.Padding = new Padding(0, 0, 8, 0);
        lblCustomer.Size = new Size(124, 30);
        lblCustomer.TabIndex = 0;
        lblCustomer.Text = "Customer:";
        // 
        // lblOutstanding
        // 
        lblOutstanding.Appearance.Font = new Font("Segoe UI", 9.5F);
        lblOutstanding.Appearance.Options.UseFont = true;
        lblOutstanding.Appearance.Options.UseTextOptions = true;
        lblOutstanding.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblOutstanding.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblOutstanding.Dock = DockStyle.Fill;
        lblOutstanding.Location = new Point(3, 39);
        lblOutstanding.Name = "lblOutstanding";
        lblOutstanding.Padding = new Padding(0, 0, 8, 0);
        lblOutstanding.Size = new Size(124, 30);
        lblOutstanding.TabIndex = 2;
        lblOutstanding.Text = "Outstanding Balance:";
        // 
        // lblAmount
        // 
        lblAmount.Appearance.Font = new Font("Segoe UI", 9.5F);
        lblAmount.Appearance.Options.UseFont = true;
        lblAmount.Appearance.Options.UseTextOptions = true;
        lblAmount.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblAmount.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblAmount.Dock = DockStyle.Fill;
        lblAmount.Location = new Point(3, 75);
        lblAmount.Name = "lblAmount";
        lblAmount.Padding = new Padding(0, 0, 8, 0);
        lblAmount.Size = new Size(124, 30);
        lblAmount.TabIndex = 4;
        lblAmount.Text = "Payment Amount:";
        // 
        // lblPaymentMethod
        // 
        lblPaymentMethod.Appearance.Font = new Font("Segoe UI", 9.5F);
        lblPaymentMethod.Appearance.Options.UseFont = true;
        lblPaymentMethod.Appearance.Options.UseTextOptions = true;
        lblPaymentMethod.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblPaymentMethod.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblPaymentMethod.Dock = DockStyle.Fill;
        lblPaymentMethod.Location = new Point(3, 111);
        lblPaymentMethod.Name = "lblPaymentMethod";
        lblPaymentMethod.Padding = new Padding(0, 0, 8, 0);
        lblPaymentMethod.Size = new Size(124, 30);
        lblPaymentMethod.TabIndex = 6;
        lblPaymentMethod.Text = "Payment Method:";
        // 
        // lblReference
        // 
        lblReference.Appearance.Font = new Font("Segoe UI", 9.5F);
        lblReference.Appearance.Options.UseFont = true;
        lblReference.Appearance.Options.UseTextOptions = true;
        lblReference.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblReference.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblReference.Dock = DockStyle.Fill;
        lblReference.Location = new Point(3, 147);
        lblReference.Name = "lblReference";
        lblReference.Padding = new Padding(0, 0, 8, 0);
        lblReference.Size = new Size(124, 30);
        lblReference.TabIndex = 8;
        lblReference.Text = "Reference / Slip #:";
        // 
        // lblNotes
        // 
        lblNotes.Appearance.Font = new Font("Segoe UI", 9.5F);
        lblNotes.Appearance.Options.UseFont = true;
        lblNotes.Appearance.Options.UseTextOptions = true;
        lblNotes.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblNotes.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblNotes.Dock = DockStyle.Fill;
        lblNotes.Location = new Point(3, 183);
        lblNotes.Name = "lblNotes";
        lblNotes.Padding = new Padding(0, 0, 8, 0);
        lblNotes.Size = new Size(124, 84);
        lblNotes.TabIndex = 10;
        lblNotes.Text = "Notes / Description:";
        // 
        // btnPanel
        // 
        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnSubmit);
        btnPanel.Dock = DockStyle.Fill;
        btnPanel.FlowDirection = FlowDirection.RightToLeft;
        btnPanel.Location = new Point(19, 295);
        btnPanel.Name = "btnPanel";
        btnPanel.Padding = new Padding(0, 4, 0, 0);
        btnPanel.Size = new Size(420, 44);
        btnPanel.TabIndex = 1;
        // 
        // CustomerPaymentForm
        // 
        ClientSize = new Size(458, 358);
        Controls.Add(root);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(460, 390);
        Name = "CustomerPaymentForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Receive Customer Payment";
        Load += CustomerPaymentForm_Load;
        ((System.ComponentModel.ISupportInitialize)_txtCustomer.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtOutstanding.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_spinAmount.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_comboPaymentMethod.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtReference.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtNotes.Properties).EndInit();
        root.ResumeLayout(false);
        fieldTable.ResumeLayout(false);
        fieldTable.PerformLayout();
        btnPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
