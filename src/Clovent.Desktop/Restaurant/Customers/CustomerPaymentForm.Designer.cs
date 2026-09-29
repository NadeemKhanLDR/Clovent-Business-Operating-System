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
    private TextEdit _txtAdvance;
    private SpinEdit _spinAmount;
    private TextEdit _txtApplied;
    private TextEdit _txtNewAdvance;
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
    private LabelControl lblAdvance;
    private LabelControl lblAmount;
    private LabelControl lblApplied;
    private LabelControl lblNewAdvance;
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
        _txtAdvance = new TextEdit();
        _spinAmount = new SpinEdit();
        _txtApplied = new TextEdit();
        _txtNewAdvance = new TextEdit();
        _comboPaymentMethod = new ComboBoxEdit();
        _txtReference = new TextEdit();
        _txtNotes = new MemoEdit();
        _btnCancel = new SimpleButton();
        _btnSubmit = new SimpleButton();
        root = new TableLayoutPanel();
        fieldTable = new TableLayoutPanel();
        lblCustomer = new LabelControl();
        lblOutstanding = new LabelControl();
        lblAdvance = new LabelControl();
        lblAmount = new LabelControl();
        lblApplied = new LabelControl();
        lblNewAdvance = new LabelControl();
        lblPaymentMethod = new LabelControl();
        lblReference = new LabelControl();
        lblNotes = new LabelControl();
        btnPanel = new FlowLayoutPanel();

        ((System.ComponentModel.ISupportInitialize)_txtCustomer.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtOutstanding.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtAdvance.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_spinAmount.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtApplied.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtNewAdvance.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_comboPaymentMethod.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtReference.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtNotes.Properties).BeginInit();
        root.SuspendLayout();
        fieldTable.SuspendLayout();
        btnPanel.SuspendLayout();
        SuspendLayout();

        // _txtCustomer
        _txtCustomer.Dock = DockStyle.Fill;
        _txtCustomer.Location = new Point(155, 3);
        _txtCustomer.Name = "_txtCustomer";
        _txtCustomer.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtCustomer.Properties.Appearance.Options.UseFont = true;
        _txtCustomer.Properties.ReadOnly = true;
        _txtCustomer.Size = new Size(270, 24);
        _txtCustomer.TabIndex = 1;

        // _txtOutstanding
        _txtOutstanding.Dock = DockStyle.Fill;
        _txtOutstanding.Location = new Point(155, 37);
        _txtOutstanding.Name = "_txtOutstanding";
        _txtOutstanding.Properties.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _txtOutstanding.Properties.Appearance.Options.UseFont = true;
        _txtOutstanding.Properties.ReadOnly = true;
        _txtOutstanding.Size = new Size(270, 24);
        _txtOutstanding.TabIndex = 3;

        // _txtAdvance
        _txtAdvance.Dock = DockStyle.Fill;
        _txtAdvance.Location = new Point(155, 71);
        _txtAdvance.Name = "_txtAdvance";
        _txtAdvance.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtAdvance.Properties.Appearance.Options.UseFont = true;
        _txtAdvance.Properties.ReadOnly = true;
        _txtAdvance.Size = new Size(270, 24);
        _txtAdvance.TabIndex = 5;

        // _spinAmount
        _spinAmount.Dock = DockStyle.Fill;
        _spinAmount.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
        _spinAmount.Location = new Point(155, 105);
        _spinAmount.Name = "_spinAmount";
        _spinAmount.Properties.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _spinAmount.Properties.Appearance.Options.UseFont = true;
        _spinAmount.Properties.Mask.EditMask = "n2";
        _spinAmount.Properties.Mask.UseMaskAsDisplayFormat = true;
        _spinAmount.Properties.MaxValue = new decimal(new int[] { 99999999, 0, 0, 0 });
        _spinAmount.Properties.MinValue = new decimal(new int[] { 0, 0, 0, 0 });
        _spinAmount.Size = new Size(270, 24);
        _spinAmount.TabIndex = 7;

        // _txtApplied
        _txtApplied.Dock = DockStyle.Fill;
        _txtApplied.Location = new Point(155, 139);
        _txtApplied.Name = "_txtApplied";
        _txtApplied.Properties.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _txtApplied.Properties.Appearance.ForeColor = Color.FromArgb(22, 101, 52);
        _txtApplied.Properties.Appearance.Options.UseFont = true;
        _txtApplied.Properties.Appearance.Options.UseForeColor = true;
        _txtApplied.Properties.ReadOnly = true;
        _txtApplied.Size = new Size(270, 24);
        _txtApplied.TabIndex = 9;

        // _txtNewAdvance
        _txtNewAdvance.Dock = DockStyle.Fill;
        _txtNewAdvance.Location = new Point(155, 173);
        _txtNewAdvance.Name = "_txtNewAdvance";
        _txtNewAdvance.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtNewAdvance.Properties.Appearance.ForeColor = Color.FromArgb(30, 64, 175);
        _txtNewAdvance.Properties.Appearance.Options.UseFont = true;
        _txtNewAdvance.Properties.Appearance.Options.UseForeColor = true;
        _txtNewAdvance.Properties.ReadOnly = true;
        _txtNewAdvance.Size = new Size(270, 24);
        _txtNewAdvance.TabIndex = 11;

        // _comboPaymentMethod
        _comboPaymentMethod.Dock = DockStyle.Fill;
        _comboPaymentMethod.Location = new Point(155, 207);
        _comboPaymentMethod.Name = "_comboPaymentMethod";
        _comboPaymentMethod.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _comboPaymentMethod.Properties.Appearance.Options.UseFont = true;
        _comboPaymentMethod.Properties.Items.AddRange(new object[] { "Cash", "Credit Card" });
        _comboPaymentMethod.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _comboPaymentMethod.Size = new Size(270, 24);
        _comboPaymentMethod.TabIndex = 13;

        // _txtReference
        _txtReference.Dock = DockStyle.Fill;
        _txtReference.Location = new Point(155, 241);
        _txtReference.Name = "_txtReference";
        _txtReference.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtReference.Properties.Appearance.Options.UseFont = true;
        _txtReference.Size = new Size(270, 24);
        _txtReference.TabIndex = 15;

        // _txtNotes
        _txtNotes.Dock = DockStyle.Fill;
        _txtNotes.Location = new Point(155, 275);
        _txtNotes.Name = "_txtNotes";
        _txtNotes.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _txtNotes.Properties.Appearance.Options.UseFont = true;
        _txtNotes.Size = new Size(270, 60);
        _txtNotes.TabIndex = 17;

        // _btnCancel
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Location = new Point(340, 7);
        _btnCancel.MinimumSize = new Size(95, 34);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new Size(95, 34);
        _btnCancel.TabIndex = 0;
        _btnCancel.Text = "Cancel";

        // _btnSubmit
        _btnSubmit.Location = new Point(190, 7);
        _btnSubmit.MinimumSize = new Size(140, 34);
        _btnSubmit.Name = "_btnSubmit";
        _btnSubmit.Size = new Size(140, 34);
        _btnSubmit.TabIndex = 1;
        _btnSubmit.Text = "Record Payment";
        _btnSubmit.Click += BtnSubmit_Click;

        // root
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(fieldTable, 0, 0);
        root.Controls.Add(btnPanel, 0, 1);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Name = "root";
        root.Padding = new Padding(16);
        root.RowCount = 2;
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        root.Size = new Size(490, 430);
        root.TabIndex = 0;

        // fieldTable
        fieldTable.ColumnCount = 2;
        fieldTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        fieldTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        fieldTable.Controls.Add(lblCustomer, 0, 0);
        fieldTable.Controls.Add(_txtCustomer, 1, 0);
        fieldTable.Controls.Add(lblOutstanding, 0, 1);
        fieldTable.Controls.Add(_txtOutstanding, 1, 1);
        fieldTable.Controls.Add(lblAdvance, 0, 2);
        fieldTable.Controls.Add(_txtAdvance, 1, 2);
        fieldTable.Controls.Add(lblAmount, 0, 3);
        fieldTable.Controls.Add(_spinAmount, 1, 3);
        fieldTable.Controls.Add(lblApplied, 0, 4);
        fieldTable.Controls.Add(_txtApplied, 1, 4);
        fieldTable.Controls.Add(lblNewAdvance, 0, 5);
        fieldTable.Controls.Add(_txtNewAdvance, 1, 5);
        fieldTable.Controls.Add(lblPaymentMethod, 0, 6);
        fieldTable.Controls.Add(_comboPaymentMethod, 1, 6);
        fieldTable.Controls.Add(lblReference, 0, 7);
        fieldTable.Controls.Add(_txtReference, 1, 7);
        fieldTable.Controls.Add(lblNotes, 0, 8);
        fieldTable.Controls.Add(_txtNotes, 1, 8);
        fieldTable.Dock = DockStyle.Fill;
        fieldTable.Location = new Point(19, 19);
        fieldTable.Name = "fieldTable";
        fieldTable.RowCount = 9;
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        fieldTable.Size = new Size(452, 345);
        fieldTable.TabIndex = 0;

        // Labels
        void ConfigureLabel(LabelControl lbl, string text)
        {
            lbl.Appearance.Font = new Font("Segoe UI", 9.5F);
            lbl.Appearance.Options.UseFont = true;
            lbl.Appearance.Options.UseTextOptions = true;
            lbl.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            lbl.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            lbl.Dock = DockStyle.Fill;
            lbl.Padding = new Padding(0, 0, 8, 0);
            lbl.Text = text;
        }

        ConfigureLabel(lblCustomer, "Customer:");
        ConfigureLabel(lblOutstanding, "Outstanding A/R:");
        ConfigureLabel(lblAdvance, "Advance Balance:");
        ConfigureLabel(lblAmount, "Payment Amount *:");
        ConfigureLabel(lblApplied, "Applied to A/R:");
        ConfigureLabel(lblNewAdvance, "New Advance:");
        ConfigureLabel(lblPaymentMethod, "Payment Method *:");
        ConfigureLabel(lblReference, "Reference #:");
        ConfigureLabel(lblNotes, "Notes:");

        // btnPanel
        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnSubmit);
        btnPanel.Dock = DockStyle.Fill;
        btnPanel.FlowDirection = FlowDirection.RightToLeft;
        btnPanel.Location = new Point(19, 365);
        btnPanel.Name = "btnPanel";
        btnPanel.Padding = new Padding(0, 6, 0, 0);
        btnPanel.Size = new Size(452, 44);
        btnPanel.TabIndex = 1;

        // CustomerPaymentForm
        AcceptButton = _btnSubmit;
        CancelButton = _btnCancel;
        ClientSize = new Size(490, 430);
        Controls.Add(root);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "CustomerPaymentForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Receive Customer Payment";
        Load += CustomerPaymentForm_Load;

        ((System.ComponentModel.ISupportInitialize)_txtCustomer.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtOutstanding.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtAdvance.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_spinAmount.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtApplied.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtNewAdvance.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_comboPaymentMethod.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtReference.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtNotes.Properties).EndInit();
        root.ResumeLayout(false);
        fieldTable.ResumeLayout(false);
        btnPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
