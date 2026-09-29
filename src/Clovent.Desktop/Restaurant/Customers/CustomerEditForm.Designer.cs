using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Customers;

partial class CustomerEditForm
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

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor. Designer safe layout.
    /// </summary>
    private void InitializeComponent()
    {
        _pnlHeader = new TableLayoutPanel();
        _lblHeaderTitle = new LabelControl();
        _lblHeaderSubtitle = new LabelControl();

        _lblSectionDetails = new LabelControl();
        label1 = new System.Windows.Forms.Label();
        label2 = new System.Windows.Forms.Label();
        label3 = new System.Windows.Forms.Label();
        labelMobile2 = new System.Windows.Forms.Label();
        labelPhone = new System.Windows.Forms.Label();
        labelShopNo = new System.Windows.Forms.Label();
        label4 = new System.Windows.Forms.Label();
        label5 = new System.Windows.Forms.Label();

        _lblSectionAccount = new LabelControl();
        label6 = new System.Windows.Forms.Label();

        _lblSectionCredit = new LabelControl();
        label7 = new System.Windows.Forms.Label();
        labelCreditLimitHelp = new System.Windows.Forms.Label();
        labelCreditAllowed = new System.Windows.Forms.Label();
        labelDefault = new System.Windows.Forms.Label();

        _lblSectionNotes = new LabelControl();
        label8 = new System.Windows.Forms.Label();

        _codeEdit = new DevExpress.XtraEditors.TextEdit();
        _nameEdit = new DevExpress.XtraEditors.TextEdit();
        _mobileEdit = new DevExpress.XtraEditors.TextEdit();
        _mobile2Edit = new DevExpress.XtraEditors.TextEdit();
        _phoneEdit = new DevExpress.XtraEditors.TextEdit();
        _shopNoEdit = new DevExpress.XtraEditors.TextEdit();
        _addressEdit = new DevExpress.XtraEditors.TextEdit();
        _emailEdit = new DevExpress.XtraEditors.TextEdit();
        _openingBalanceEdit = new DevExpress.XtraEditors.SpinEdit();
        _creditLimitEdit = new DevExpress.XtraEditors.SpinEdit();
        _isCreditAllowedCheck = new DevExpress.XtraEditors.CheckEdit();
        _isDefaultCheck = new DevExpress.XtraEditors.CheckEdit();
        _notesEdit = new DevExpress.XtraEditors.TextEdit();

        ((System.ComponentModel.ISupportInitialize)_codeEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_mobileEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_mobile2Edit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_phoneEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_shopNoEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_addressEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_emailEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_openingBalanceEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_creditLimitEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_isCreditAllowedCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_isDefaultCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_notesEdit.Properties).BeginInit();

        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.AutoScroll = false;
        _contentPanel.AutoSize = false;
        _contentPanel.Padding = new Padding(16, 8, 16, 8);
        _contentPanel.ColumnCount = 4;
        _contentPanel.ColumnStyles.Clear();
        _contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145F));
        _contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145F));
        _contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        _contentPanel.RowCount = 13;
        _contentPanel.RowStyles.Clear();
        for (int i = 0; i < 13; i++)
        {
            _contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        int row = 0;

        // --- ROW 0: FORM BANNER / HEADER (spans 4 cols) ---
        _pnlHeader.ColumnCount = 1;
        _pnlHeader.RowCount = 2;
        _pnlHeader.AutoSize = true;
        _pnlHeader.Dock = DockStyle.Top;
        _pnlHeader.Margin = new Padding(0, 0, 0, 6);
        _lblHeaderTitle.Text = "EDIT CUSTOMER";
        _lblHeaderTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _lblHeaderTitle.ForeColor = Color.FromArgb(15, 23, 42);
        _lblHeaderTitle.Margin = new Padding(0, 0, 0, 2);
        _lblHeaderSubtitle.Text = "Maintain customer contact and account settings.";
        _lblHeaderSubtitle.Font = new Font("Segoe UI", 8.75F, FontStyle.Regular);
        _lblHeaderSubtitle.ForeColor = Color.FromArgb(100, 116, 139);
        _pnlHeader.Controls.Add(_lblHeaderTitle, 0, 0);
        _pnlHeader.Controls.Add(_lblHeaderSubtitle, 0, 1);
        _contentPanel.Controls.Add(_pnlHeader, 0, row);
        _contentPanel.SetColumnSpan(_pnlHeader, 4);
        row++;

        // --- [ CUSTOMER DETAILS ] SECTION HEADER (spans 4 cols) ---
        _lblSectionDetails.Text = "CUSTOMER DETAILS";
        _lblSectionDetails.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblSectionDetails.ForeColor = Color.FromArgb(14, 116, 144); // Slate teal
        _lblSectionDetails.Dock = DockStyle.Top;
        _lblSectionDetails.Padding = new Padding(0, 2, 0, 2);
        _lblSectionDetails.Margin = new Padding(0, 2, 0, 4);
        _contentPanel.Controls.Add(_lblSectionDetails, 0, row);
        _contentPanel.SetColumnSpan(_lblSectionDetails, 4);
        row++;

        // Customer Code & Customer Name
        ConfigureField(label1, "Customer Code *:", _codeEdit, 0, row);
        ConfigureField(label2, "Customer Name *:", _nameEdit, 2, row);
        row++;

        // Mobile Number & Mobile 2
        ConfigureField(label3, "Mobile Number *:", _mobileEdit, 0, row);
        ConfigureField(labelMobile2, "Mobile 2:", _mobile2Edit, 2, row);
        row++;

        // Phone & Shop No
        ConfigureField(labelPhone, "Phone:", _phoneEdit, 0, row);
        ConfigureField(labelShopNo, "Shop No:", _shopNoEdit, 2, row);
        row++;

        // Address & Email
        ConfigureField(label4, "Address *:", _addressEdit, 0, row);
        ConfigureField(label5, "Email:", _emailEdit, 2, row);
        row++;

        // --- [ ACCOUNT OPENING ] SECTION HEADER (spans 4 cols) ---
        _lblSectionAccount.Text = "ACCOUNT OPENING";
        _lblSectionAccount.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblSectionAccount.ForeColor = Color.FromArgb(14, 116, 144);
        _lblSectionAccount.Dock = DockStyle.Top;
        _lblSectionAccount.Padding = new Padding(0, 2, 0, 2);
        _lblSectionAccount.Margin = new Padding(0, 6, 0, 4);
        _contentPanel.Controls.Add(_lblSectionAccount, 0, row);
        _contentPanel.SetColumnSpan(_lblSectionAccount, 4);
        row++;

        // Opening Balance
        ConfigureField(label6, "Opening Balance:", _openingBalanceEdit, 0, row);
        row++;

        // --- [ CREDIT / ACCOUNT SETTINGS ] SECTION HEADER (spans 4 cols) ---
        _lblSectionCredit.Text = "CREDIT / ACCOUNT SETTINGS";
        _lblSectionCredit.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblSectionCredit.ForeColor = Color.FromArgb(14, 116, 144);
        _lblSectionCredit.Dock = DockStyle.Top;
        _lblSectionCredit.Padding = new Padding(0, 2, 0, 2);
        _lblSectionCredit.Margin = new Padding(0, 6, 0, 4);
        _contentPanel.Controls.Add(_lblSectionCredit, 0, row);
        _contentPanel.SetColumnSpan(_lblSectionCredit, 4);
        row++;

        // Credit Limit + Helper text in Col 0,1 & Credit Allowed in Col 2,3
        var pnlCreditLimit = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 3, 16, 6)
        };
        pnlCreditLimit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pnlCreditLimit.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pnlCreditLimit.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _creditLimitEdit.Dock = DockStyle.Top;
        _creditLimitEdit.Margin = new Padding(0, 0, 0, 5);

        labelCreditLimitHelp.Dock = DockStyle.Top;
        labelCreditLimitHelp.Text = "0.00 = Zero Credit Limit (no credit extended).";
        labelCreditLimitHelp.Font = new Font("Segoe UI", 8.25F, FontStyle.Italic);
        labelCreditLimitHelp.ForeColor = Color.DimGray;
        labelCreditLimitHelp.Padding = new Padding(0, 2, 0, 2);
        labelCreditLimitHelp.Margin = new Padding(0, 4, 0, 8);
        labelCreditLimitHelp.AutoSize = true;

        pnlCreditLimit.Controls.Add(_creditLimitEdit, 0, 0);
        pnlCreditLimit.Controls.Add(labelCreditLimitHelp, 0, 1);

        label7.AutoSize = false;
        label7.Dock = DockStyle.Fill;
        label7.Text = "Credit Limit:";
        label7.TextAlign = ContentAlignment.TopLeft;
        label7.Padding = new Padding(0, 5, 6, 0);
        label7.Margin = new Padding(0, 3, 6, 4);

        _contentPanel.Controls.Add(label7, 0, row);
        _contentPanel.Controls.Add(pnlCreditLimit, 1, row);

        // Credit Allowed in Col 2, 3
        labelCreditAllowed.AutoSize = false;
        labelCreditAllowed.Dock = DockStyle.Fill;
        labelCreditAllowed.Text = "Credit Facility:";
        labelCreditAllowed.TextAlign = ContentAlignment.TopLeft;
        labelCreditAllowed.Padding = new Padding(0, 5, 6, 0);
        labelCreditAllowed.Margin = new Padding(0, 3, 6, 4);
        _isCreditAllowedCheck.Dock = DockStyle.Top;
        _isCreditAllowedCheck.Margin = new Padding(0, 3, 0, 2);
        _isCreditAllowedCheck.Properties.Caption = "Allow on-account purchases";
        _contentPanel.Controls.Add(labelCreditAllowed, 2, row);
        _contentPanel.Controls.Add(_isCreditAllowedCheck, 3, row);
        row++;

        // Default Customer
        labelDefault.AutoSize = false;
        labelDefault.Dock = DockStyle.Fill;
        labelDefault.Text = "Default Customer:";
        labelDefault.TextAlign = ContentAlignment.MiddleLeft;
        labelDefault.Padding = new Padding(0, 0, 6, 0);
        labelDefault.Margin = new Padding(0, 4, 6, 4);
        _isDefaultCheck.Dock = DockStyle.Top;
        _isDefaultCheck.Margin = new Padding(0, 4, 0, 4);
        _isDefaultCheck.Properties.Caption = "Set as default customer for new POS orders";
        _contentPanel.Controls.Add(labelDefault, 0, row);
        _contentPanel.Controls.Add(_isDefaultCheck, 1, row);
        _contentPanel.SetColumnSpan(_isDefaultCheck, 3);
        row++;

        // --- [ NOTES ] SECTION HEADER (spans 4 cols) ---
        _lblSectionNotes.Text = "NOTES";
        _lblSectionNotes.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblSectionNotes.ForeColor = Color.FromArgb(14, 116, 144);
        _lblSectionNotes.Dock = DockStyle.Top;
        _lblSectionNotes.Padding = new Padding(0, 2, 0, 2);
        _lblSectionNotes.Margin = new Padding(0, 6, 0, 4);
        _contentPanel.Controls.Add(_lblSectionNotes, 0, row);
        _contentPanel.SetColumnSpan(_lblSectionNotes, 4);
        row++;

        // Notes TextEdit (compact single-line editor)
        label8.AutoSize = false;
        label8.Dock = DockStyle.Fill;
        label8.Text = "Notes:";
        label8.TextAlign = ContentAlignment.MiddleLeft;
        label8.Padding = new Padding(0, 0, 6, 0);
        label8.Margin = new Padding(0, 3, 6, 4);
        _notesEdit.Dock = DockStyle.Fill;
        _notesEdit.Margin = new Padding(0, 3, 0, 4);
        _contentPanel.Controls.Add(label8, 0, row);
        _contentPanel.Controls.Add(_notesEdit, 1, row);
        _contentPanel.SetColumnSpan(_notesEdit, 3);

        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();

        // Control properties
        _codeEdit.Name = "_codeEdit";
        _nameEdit.Name = "_nameEdit";
        _mobileEdit.Name = "_mobileEdit";
        _mobile2Edit.Name = "_mobile2Edit";
        _phoneEdit.Name = "_phoneEdit";
        _shopNoEdit.Name = "_shopNoEdit";
        _addressEdit.Name = "_addressEdit";
        _emailEdit.Name = "_emailEdit";

        _openingBalanceEdit.Name = "_openingBalanceEdit";
        _openingBalanceEdit.Properties.Buttons.Clear();
        _openingBalanceEdit.Properties.Mask.EditMask = "n2";
        _openingBalanceEdit.Properties.Mask.UseMaskAsDisplayFormat = true;

        _creditLimitEdit.Name = "_creditLimitEdit";
        _creditLimitEdit.Properties.Buttons.Clear();
        _creditLimitEdit.Properties.Mask.EditMask = "n2";
        _creditLimitEdit.Properties.Mask.UseMaskAsDisplayFormat = true;

        _isCreditAllowedCheck.Name = "_isCreditAllowedCheck";
        _isDefaultCheck.Name = "_isDefaultCheck";
        _notesEdit.Name = "_notesEdit";

        ((System.ComponentModel.ISupportInitialize)_codeEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_mobileEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_mobile2Edit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_phoneEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_shopNoEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_addressEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_emailEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_openingBalanceEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_creditLimitEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_isCreditAllowedCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_isDefaultCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_notesEdit.Properties).EndInit();

        ClientSize = new Size(780, 460);
        MinimumSize = new Size(740, 430);
        ResumeLayout(false);
    }

    private void ConfigureField(System.Windows.Forms.Label label, string labelText, Control editor, int col, int row)
    {
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Text = labelText;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Padding = new Padding(0, 0, 6, 0);
        label.Margin = new Padding(0, 3, 6, 4);

        editor.Dock = DockStyle.Fill;
        editor.Margin = new Padding(0, 3, col == 0 ? 16 : 0, 4);

        _contentPanel.Controls.Add(label, col, row);
        _contentPanel.Controls.Add(editor, col + 1, row);
    }

    private TableLayoutPanel _pnlHeader;
    private LabelControl _lblHeaderTitle;
    private LabelControl _lblHeaderSubtitle;
    private LabelControl _lblSectionDetails;
    private LabelControl _lblSectionAccount;
    private LabelControl _lblSectionCredit;
    private LabelControl _lblSectionNotes;

    private DevExpress.XtraEditors.TextEdit _codeEdit;
    private DevExpress.XtraEditors.TextEdit _nameEdit;
    private DevExpress.XtraEditors.TextEdit _mobileEdit;
    private DevExpress.XtraEditors.TextEdit _mobile2Edit;
    private DevExpress.XtraEditors.TextEdit _phoneEdit;
    private DevExpress.XtraEditors.TextEdit _shopNoEdit;
    private DevExpress.XtraEditors.TextEdit _addressEdit;
    private DevExpress.XtraEditors.TextEdit _emailEdit;
    private DevExpress.XtraEditors.SpinEdit _openingBalanceEdit;
    private DevExpress.XtraEditors.SpinEdit _creditLimitEdit;
    private DevExpress.XtraEditors.CheckEdit _isCreditAllowedCheck;
    private DevExpress.XtraEditors.CheckEdit _isDefaultCheck;
    private DevExpress.XtraEditors.TextEdit _notesEdit;

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label labelMobile2;
    private System.Windows.Forms.Label labelPhone;
    private System.Windows.Forms.Label labelShopNo;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.Label labelCreditLimitHelp;
    private System.Windows.Forms.Label labelCreditAllowed;
    private System.Windows.Forms.Label labelDefault;
    private System.Windows.Forms.Label label8;
}
