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
        btnPanel = new FlowLayoutPanel();
        lblCustomer = new LabelControl();
        lblOutstanding = new LabelControl();
        lblAmount = new LabelControl();
        lblPaymentMethod = new LabelControl();
        lblReference = new LabelControl();
        lblNotes = new LabelControl();

        ((System.ComponentModel.ISupportInitialize)_txtCustomer.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtOutstanding.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_spinAmount.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_comboPaymentMethod.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtReference.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtNotes.Properties).BeginInit();
        SuspendLayout();

        // Form settings
        Text = "Receive Customer Payment";
        Size = new Size(460, 390);
        MinimumSize = new Size(460, 390);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Name = "CustomerPaymentForm";

        // Layout Panel
        root.Dock = DockStyle.Fill;
        root.ColumnCount = 1;
        root.RowCount = 2;
        root.Padding = new Padding(16);
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Input Fields Group
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));   // Action Buttons

        fieldTable.Dock = DockStyle.Fill;
        fieldTable.ColumnCount = 2;
        fieldTable.RowCount = 6;
        fieldTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        fieldTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        fieldTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F)); // MemoEdit Notes row

        // _txtCustomer
        _txtCustomer.Name = "_txtCustomer";
        _txtCustomer.Properties.ReadOnly = true;
        _txtCustomer.Dock = DockStyle.Fill;
        _txtCustomer.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblCustomer.Text = "Customer:";
        lblCustomer.Dock = DockStyle.Fill;
        lblCustomer.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblCustomer.Padding = new Padding(0, 0, 8, 0);
        lblCustomer.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblCustomer.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        fieldTable.Controls.Add(lblCustomer, 0, 0);
        fieldTable.Controls.Add(_txtCustomer, 1, 0);

        // _txtOutstanding
        _txtOutstanding.Name = "_txtOutstanding";
        _txtOutstanding.Properties.ReadOnly = true;
        _txtOutstanding.Properties.AppearanceReadOnly.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _txtOutstanding.Properties.AppearanceReadOnly.Options.UseFont = true;
        _txtOutstanding.Dock = DockStyle.Fill;
        _txtOutstanding.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblOutstanding.Text = "Outstanding Balance:";
        lblOutstanding.Dock = DockStyle.Fill;
        lblOutstanding.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblOutstanding.Padding = new Padding(0, 0, 8, 0);
        lblOutstanding.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblOutstanding.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        fieldTable.Controls.Add(lblOutstanding, 0, 1);
        fieldTable.Controls.Add(_txtOutstanding, 1, 1);

        // _spinAmount
        _spinAmount.Name = "_spinAmount";
        _spinAmount.Properties.Buttons.Clear();
        _spinAmount.Properties.Mask.EditMask = "n2";
        _spinAmount.Properties.Mask.UseMaskAsDisplayFormat = true;
        _spinAmount.Properties.MinValue = 0.01m;
        _spinAmount.Properties.MaxValue = 99999999m;
        _spinAmount.Dock = DockStyle.Fill;
        _spinAmount.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblAmount.Text = "Payment Amount:";
        lblAmount.Dock = DockStyle.Fill;
        lblAmount.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblAmount.Padding = new Padding(0, 0, 8, 0);
        lblAmount.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblAmount.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        fieldTable.Controls.Add(lblAmount, 0, 2);
        fieldTable.Controls.Add(_spinAmount, 1, 2);

        // _comboPaymentMethod
        _comboPaymentMethod.Name = "_comboPaymentMethod";
        _comboPaymentMethod.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        // Design-surface placeholders only - the runtime constructor clears
        // these and loads the owner's configured payment methods instead.
        _comboPaymentMethod.Properties.Items.AddRange(new object[] { "Cash", "Credit", "Credit Card" });
        _comboPaymentMethod.Dock = DockStyle.Fill;
        _comboPaymentMethod.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblPaymentMethod.Text = "Payment Method:";
        lblPaymentMethod.Dock = DockStyle.Fill;
        lblPaymentMethod.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblPaymentMethod.Padding = new Padding(0, 0, 8, 0);
        lblPaymentMethod.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblPaymentMethod.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        fieldTable.Controls.Add(lblPaymentMethod, 0, 3);
        fieldTable.Controls.Add(_comboPaymentMethod, 1, 3);

        // _txtReference
        _txtReference.Name = "_txtReference";
        _txtReference.Dock = DockStyle.Fill;
        _txtReference.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblReference.Text = "Reference / Slip #:";
        lblReference.Dock = DockStyle.Fill;
        lblReference.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblReference.Padding = new Padding(0, 0, 8, 0);
        lblReference.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblReference.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        fieldTable.Controls.Add(lblReference, 0, 4);
        fieldTable.Controls.Add(_txtReference, 1, 4);

        // _txtNotes
        _txtNotes.Name = "_txtNotes";
        _txtNotes.Dock = DockStyle.Fill;
        _txtNotes.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblNotes.Text = "Notes / Description:";
        lblNotes.Dock = DockStyle.Fill;
        lblNotes.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        lblNotes.Padding = new Padding(0, 0, 8, 0);
        lblNotes.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblNotes.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        fieldTable.Controls.Add(lblNotes, 0, 5);
        fieldTable.Controls.Add(_txtNotes, 1, 5);

        // Action Buttons Panel
        btnPanel.Dock = DockStyle.Fill;
        btnPanel.FlowDirection = FlowDirection.RightToLeft;
        btnPanel.Padding = new Padding(0, 4, 0, 0);

        _btnCancel.Text = "Cancel";
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.MinimumSize = new Size(95, 34);

        _btnSubmit.Text = "Receive Payment";
        _btnSubmit.MinimumSize = new Size(130, 34);
        _btnSubmit.Click += BtnSubmit_Click;

        btnPanel.Controls.Add(_btnCancel);
        btnPanel.Controls.Add(_btnSubmit);

        // Assemble root
        root.Controls.Add(fieldTable, 0, 0);
        root.Controls.Add(btnPanel, 0, 1);
        Controls.Add(root);

        ((System.ComponentModel.ISupportInitialize)_txtCustomer.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtOutstanding.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_spinAmount.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_comboPaymentMethod.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtReference.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtNotes.Properties).EndInit();

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += CustomerPaymentForm_Load;

        ResumeLayout(false);
    }
}
