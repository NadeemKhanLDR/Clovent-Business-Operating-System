using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

partial class DeliveryDetailsDialog
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel root;
    private TableLayoutPanel topPanel;
    private TableLayoutPanel formPanel;
    private FlowLayoutPanel bottomPanel;

    private LabelControl headerLabel;
    private LabelControl subheaderLabel;

    private LabelControl _lblCustomer;
    private TableLayoutPanel _customerPickerPanel;
    private SearchLookUpEdit _customerLookup;
    private SimpleButton _btnNewCustomer;

    private LabelControl _lblName;
    private TextEdit _txtName;

    private LabelControl _lblPhone;
    private TextEdit _txtPhone;

    private LabelControl _lblAddress;
    private MemoEdit _txtAddress;

    private LabelControl _lblSource;
    private ComboBoxEdit _comboSource;

    private LabelControl _lblFee;
    private SpinEdit _spinDeliveryFee;

    private LabelControl _lblRider;
    private TextEdit _txtRider;

    private LabelControl _lblRiderPhone;
    private TextEdit _txtRiderPhone;

    private LabelControl _lblNotes;
    private MemoEdit _txtNotes;

    private SimpleButton _btnConfirm;
    private SimpleButton _btnCancel;

    private void InitializeComponent()
    {
        root = new TableLayoutPanel();
        topPanel = new TableLayoutPanel();
        formPanel = new TableLayoutPanel();
        bottomPanel = new FlowLayoutPanel();

        headerLabel = new LabelControl();
        subheaderLabel = new LabelControl();

        _lblCustomer = new LabelControl();
        _customerPickerPanel = new TableLayoutPanel();
        _customerLookup = new SearchLookUpEdit();
        _btnNewCustomer = new SimpleButton();

        _lblName = new LabelControl();
        _txtName = new TextEdit();

        _lblPhone = new LabelControl();
        _txtPhone = new TextEdit();

        _lblAddress = new LabelControl();
        _txtAddress = new MemoEdit();

        _lblSource = new LabelControl();
        _comboSource = new ComboBoxEdit();

        _lblFee = new LabelControl();
        _spinDeliveryFee = new SpinEdit();

        _lblRider = new LabelControl();
        _txtRider = new TextEdit();

        _lblRiderPhone = new LabelControl();
        _txtRiderPhone = new TextEdit();

        _lblNotes = new LabelControl();
        _txtNotes = new MemoEdit();

        _btnConfirm = new SimpleButton();
        _btnCancel = new SimpleButton();

        ((System.ComponentModel.ISupportInitialize)_customerLookup.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtName.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtPhone.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtAddress.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_comboSource.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_spinDeliveryFee.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtRider.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtRiderPhone.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_txtNotes.Properties).BeginInit();
        SuspendLayout();

        // root
        root.Dock = DockStyle.Fill;
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowCount = 3;
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Padding = new Padding(18, 10, 18, 10);

        // topPanel
        topPanel.Dock = DockStyle.Top;
        topPanel.AutoSize = true;
        topPanel.ColumnCount = 1;
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        topPanel.RowCount = 2;
        topPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topPanel.Margin = new Padding(0, 0, 0, 6);

        headerLabel.Text = "Delivery Order Details";
        headerLabel.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        headerLabel.Appearance.Options.UseFont = true;

        subheaderLabel.Text = "Customer destination address, contact number, delivery fee and rider details.";
        subheaderLabel.Appearance.Font = new Font("Segoe UI", 8.75F);
        subheaderLabel.Appearance.ForeColor = Color.Gray;
        subheaderLabel.Appearance.Options.UseFont = true;
        subheaderLabel.Appearance.Options.UseForeColor = true;
        subheaderLabel.Padding = new Padding(0, 2, 0, 2);

        topPanel.Controls.Add(headerLabel, 0, 0);
        topPanel.Controls.Add(subheaderLabel, 0, 1);

        // formPanel
        formPanel.Dock = DockStyle.Fill;
        formPanel.AutoScroll = true;
        formPanel.ColumnCount = 2;
        formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        formPanel.RowCount = 9;
        formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // 0: Customer Picker
        formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // 1: Name
        formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // 2: Phone
        formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F)); // 3: Address (constrained height)
        formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // 4: Source
        formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // 5: Fee
        formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // 6: Rider Name
        formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // 7: Rider Phone
        formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F)); // 8: Special Notes (MemoEdit)

        // Customer Picker Panel
        _customerPickerPanel.Dock = DockStyle.Fill;
        _customerPickerPanel.ColumnCount = 2;
        _customerPickerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _customerPickerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75F));
        _customerPickerPanel.RowCount = 1;
        _customerPickerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _customerPickerPanel.Margin = new Padding(0, 2, 0, 4);

        _customerLookup.Dock = DockStyle.Fill;
        _customerLookup.Margin = new Padding(0, 0, 4, 0);
        _customerLookup.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _customerLookup.Properties.Appearance.Options.UseFont = true;
        _customerLookup.Properties.NullText = "Search existing customer...";

        _btnNewCustomer.Dock = DockStyle.Fill;
        _btnNewCustomer.Margin = new Padding(0);
        _btnNewCustomer.Text = "+ New";

        _customerPickerPanel.Controls.Add(_customerLookup, 0, 0);
        _customerPickerPanel.Controls.Add(_btnNewCustomer, 1, 0);

        ConfigureRow(_lblCustomer, _customerPickerPanel, "Select Customer", 0);
        ConfigureRow(_lblName, _txtName, "Customer Name *", 1);
        ConfigureRow(_lblPhone, _txtPhone, "Contact Phone *", 2);
        ConfigureRow(_lblAddress, _txtAddress, "Delivery Address *", 3);
        ConfigureRow(_lblSource, _comboSource, "Order Source", 4);
        ConfigureRow(_lblFee, _spinDeliveryFee, "Delivery Fee", 5);
        ConfigureRow(_lblRider, _txtRider, "Rider / Driver", 6);
        ConfigureRow(_lblRiderPhone, _txtRiderPhone, "Rider Phone", 7);
        ConfigureRow(_lblNotes, _txtNotes, "Special Notes", 8);

        // Align address and notes label to top of cell
        _lblAddress.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
        _lblAddress.Padding = new Padding(0, 6, 0, 0);
        _lblNotes.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
        _lblNotes.Padding = new Padding(0, 6, 0, 0);

        _comboSource.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _comboSource.Properties.Items.AddRange(new object[] { "Phone", "Online", "Walk-In" });
        _comboSource.SelectedIndex = 0;

        _spinDeliveryFee.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        _spinDeliveryFee.Properties.Mask.EditMask = "n2";
        _spinDeliveryFee.Properties.Mask.UseMaskAsDisplayFormat = true;
        _spinDeliveryFee.Value = 100.00m;

        _txtAddress.Properties.ScrollBars = ScrollBars.Vertical;
        _txtNotes.Properties.ScrollBars = ScrollBars.Vertical;
        _txtNotes.MinimumSize = new Size(0, 50);

        // bottomPanel
        bottomPanel.Dock = DockStyle.Fill;
        bottomPanel.FlowDirection = FlowDirection.RightToLeft;
        bottomPanel.AutoSize = true;
        bottomPanel.Margin = new Padding(0, 8, 0, 0);
        bottomPanel.Padding = new Padding(0, 4, 0, 0);

        _btnConfirm.Text = "Confirm Delivery";
        _btnConfirm.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _btnConfirm.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _btnConfirm.Appearance.ForeColor = Color.White;
        _btnConfirm.Appearance.Options.UseBackColor = true;
        _btnConfirm.Appearance.Options.UseForeColor = true;
        _btnConfirm.Appearance.Options.UseFont = true;
        _btnConfirm.Cursor = Cursors.Hand;
        _btnConfirm.DialogResult = DialogResult.OK;

        _btnCancel.Text = "Cancel";
        _btnCancel.Appearance.Font = new Font("Segoe UI", 9.5F);
        _btnCancel.Appearance.Options.UseFont = true;
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Margin = new Padding(8, 0, 0, 0);

        bottomPanel.Controls.Add(_btnConfirm);
        bottomPanel.Controls.Add(_btnCancel);

        root.Controls.Add(topPanel, 0, 0);
        root.Controls.Add(formPanel, 0, 1);
        root.Controls.Add(bottomPanel, 0, 2);

        Controls.Add(root);

        AcceptButton = _btnConfirm;
        CancelButton = _btnCancel;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Delivery Order Details";
        ClientSize = new Size(620, 455);
        MinimumSize = new Size(580, 420);

        ((System.ComponentModel.ISupportInitialize)_customerLookup.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtName.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtPhone.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtAddress.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_comboSource.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_spinDeliveryFee.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtRider.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtRiderPhone.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_txtNotes.Properties).EndInit();
        ResumeLayout(false);
    }

    private void ConfigureRow(LabelControl lbl, Control ctrl, string text, int row)
    {
        lbl.Text = text;
        lbl.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lbl.Appearance.ForeColor = Color.FromArgb(51, 65, 85);
        lbl.Appearance.Options.UseFont = true;
        lbl.Appearance.Options.UseForeColor = true;
        lbl.Dock = DockStyle.Fill;
        lbl.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lbl.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        lbl.Margin = new Padding(0, 0, 8, 0);

        ctrl.Dock = DockStyle.Fill;
        ctrl.Font = new Font("Segoe UI", 9.5F);
        ctrl.Margin = new Padding(0, 2, 0, 4);

        formPanel.Controls.Add(lbl, 0, row);
        formPanel.Controls.Add(ctrl, 1, row);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }
}
