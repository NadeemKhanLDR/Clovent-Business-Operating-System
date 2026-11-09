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
        _customerLayout = new TableLayoutPanel();
        _pnlHeader = new TableLayoutPanel();
        _lblHeaderTitle = new LabelControl();
        _lblHeaderSubtitle = new LabelControl();
        _lblSectionDetails = new LabelControl();
        label1 = new Label();
        _codeEdit = new TextEdit();
        label2 = new Label();
        _nameEdit = new TextEdit();
        label3 = new Label();
        _mobileEdit = new TextEdit();
        labelMobile2 = new Label();
        _mobile2Edit = new TextEdit();
        labelPhone = new Label();
        _phoneEdit = new TextEdit();
        labelShopNo = new Label();
        _shopNoEdit = new TextEdit();
        label4 = new Label();
        _addressEdit = new TextEdit();
        label5 = new Label();
        _emailEdit = new TextEdit();
        _lblSectionAccount = new LabelControl();
        label6 = new Label();
        _openingBalanceEdit = new SpinEdit();
        _lblSectionCredit = new LabelControl();
        label7 = new Label();
        pnlCreditLimit = new TableLayoutPanel();
        _creditLimitEdit = new SpinEdit();
        labelCreditLimitHelp = new Label();
        labelCreditAllowed = new Label();
        _isCreditAllowedCheck = new CheckEdit();
        labelDefault = new Label();
        _isDefaultCheck = new CheckEdit();
        _lblSectionNotes = new LabelControl();
        label8 = new Label();
        _notesEdit = new MemoEdit();
        _customerLayout.SuspendLayout();
        _pnlHeader.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_codeEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_mobileEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_mobile2Edit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_phoneEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_shopNoEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_addressEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_emailEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_openingBalanceEdit.Properties).BeginInit();
        pnlCreditLimit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_creditLimitEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_isCreditAllowedCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_isDefaultCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_notesEdit.Properties).BeginInit();
        SuspendLayout();
        //
        // _customerLayout
        //
        _customerLayout.AutoScroll = true;
        _customerLayout.ColumnCount = 4;
        _customerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145F));
        _customerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _customerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145F));
        _customerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _customerLayout.Controls.Add(_pnlHeader, 0, 0);
        _customerLayout.Controls.Add(_lblSectionDetails, 0, 1);
        _customerLayout.Controls.Add(label1, 0, 2);
        _customerLayout.Controls.Add(_codeEdit, 1, 2);
        _customerLayout.Controls.Add(label2, 2, 2);
        _customerLayout.Controls.Add(_nameEdit, 3, 2);
        _customerLayout.Controls.Add(label3, 0, 3);
        _customerLayout.Controls.Add(_mobileEdit, 1, 3);
        _customerLayout.Controls.Add(labelMobile2, 2, 3);
        _customerLayout.Controls.Add(_mobile2Edit, 3, 3);
        _customerLayout.Controls.Add(labelPhone, 0, 4);
        _customerLayout.Controls.Add(_phoneEdit, 1, 4);
        _customerLayout.Controls.Add(labelShopNo, 2, 4);
        _customerLayout.Controls.Add(_shopNoEdit, 3, 4);
        _customerLayout.Controls.Add(label4, 0, 5);
        _customerLayout.Controls.Add(_addressEdit, 1, 5);
        _customerLayout.Controls.Add(label5, 2, 5);
        _customerLayout.Controls.Add(_emailEdit, 3, 5);
        _customerLayout.Controls.Add(_lblSectionAccount, 0, 6);
        _customerLayout.Controls.Add(label6, 0, 7);
        _customerLayout.Controls.Add(_openingBalanceEdit, 1, 7);
        _customerLayout.Controls.Add(_lblSectionCredit, 0, 8);
        _customerLayout.Controls.Add(label7, 0, 9);
        _customerLayout.Controls.Add(pnlCreditLimit, 1, 9);
        _customerLayout.Controls.Add(labelCreditAllowed, 2, 9);
        _customerLayout.Controls.Add(_isCreditAllowedCheck, 3, 9);
        _customerLayout.Controls.Add(labelDefault, 0, 10);
        _customerLayout.Controls.Add(_isDefaultCheck, 1, 10);
        _customerLayout.Controls.Add(_lblSectionNotes, 0, 11);
        _customerLayout.Controls.Add(label8, 0, 12);
        _customerLayout.Controls.Add(_notesEdit, 1, 12);
        _customerLayout.Dock = DockStyle.Fill;
        _customerLayout.Location = new Point(0, 0);
        _customerLayout.Name = "_customerLayout";
        _customerLayout.Padding = new Padding(16, 8, 16, 8);
        _customerLayout.RowCount = 13;
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.RowStyles.Add(new RowStyle());
        _customerLayout.Size = new Size(820, 572);
        _customerLayout.TabIndex = 2;
        //
        // _pnlHeader
        //
        _pnlHeader.AutoSize = true;
        _pnlHeader.ColumnCount = 1;
        _customerLayout.SetColumnSpan(_pnlHeader, 4);
        _pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        _pnlHeader.Controls.Add(_lblHeaderTitle, 0, 0);
        _pnlHeader.Controls.Add(_lblHeaderSubtitle, 0, 1);
        _pnlHeader.Dock = DockStyle.Top;
        _pnlHeader.Location = new Point(16, 8);
        _pnlHeader.Margin = new Padding(0, 0, 0, 6);
        _pnlHeader.Name = "_pnlHeader";
        _pnlHeader.RowCount = 2;
        _pnlHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        _pnlHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        _pnlHeader.Size = new Size(788, 40);
        _pnlHeader.TabIndex = 0;
        //
        // _lblHeaderTitle
        //
        _lblHeaderTitle.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _lblHeaderTitle.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _lblHeaderTitle.Appearance.Options.UseFont = true;
        _lblHeaderTitle.Appearance.Options.UseForeColor = true;
        _lblHeaderTitle.Location = new Point(0, 0);
        _lblHeaderTitle.Margin = new Padding(0, 0, 0, 2);
        _lblHeaderTitle.Name = "_lblHeaderTitle";
        _lblHeaderTitle.Size = new Size(126, 21);
        _lblHeaderTitle.TabIndex = 0;
        _lblHeaderTitle.Text = "EDIT CUSTOMER";
        //
        // _lblHeaderSubtitle
        //
        _lblHeaderSubtitle.Appearance.Font = new Font("Segoe UI", 8.75F);
        _lblHeaderSubtitle.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        _lblHeaderSubtitle.Appearance.Options.UseFont = true;
        _lblHeaderSubtitle.Appearance.Options.UseForeColor = true;
        _lblHeaderSubtitle.Location = new Point(3, 23);
        _lblHeaderSubtitle.Name = "_lblHeaderSubtitle";
        _lblHeaderSubtitle.Size = new Size(259, 15);
        _lblHeaderSubtitle.TabIndex = 1;
        _lblHeaderSubtitle.Text = "Maintain customer contact and account settings.";
        //
        // _lblSectionDetails
        //
        _lblSectionDetails.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblSectionDetails.Appearance.ForeColor = Color.FromArgb(14, 116, 144);
        _lblSectionDetails.Appearance.Options.UseFont = true;
        _lblSectionDetails.Appearance.Options.UseForeColor = true;
        _customerLayout.SetColumnSpan(_lblSectionDetails, 4);
        _lblSectionDetails.Dock = DockStyle.Top;
        _lblSectionDetails.Location = new Point(16, 62);
        _lblSectionDetails.Margin = new Padding(0, 8, 0, 6);
        _lblSectionDetails.Name = "_lblSectionDetails";
        _lblSectionDetails.Padding = new Padding(0, 4, 0, 2);
        _lblSectionDetails.Size = new Size(114, 21);
        _lblSectionDetails.TabIndex = 1;
        _lblSectionDetails.Text = "CUSTOMER DETAILS";
        //
        // label1
        //
        label1.Dock = DockStyle.Fill;
        label1.Location = new Point(16, 95);
        label1.Margin = new Padding(0, 6, 6, 6);
        label1.Name = "label1";
        label1.Padding = new Padding(0, 0, 6, 0);
        label1.Size = new Size(139, 23);
        label1.TabIndex = 2;
        label1.Text = "Customer Code *:";
        label1.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _codeEdit
        //
        _codeEdit.Dock = DockStyle.Fill;
        _codeEdit.Location = new Point(161, 95);
        _codeEdit.Margin = new Padding(0, 6, 16, 6);
        _codeEdit.Name = "_codeEdit";
        _codeEdit.Size = new Size(233, 20);
        _codeEdit.TabIndex = 3;
        //
        // label2
        //
        label2.Dock = DockStyle.Fill;
        label2.Location = new Point(410, 95);
        label2.Margin = new Padding(0, 6, 6, 6);
        label2.Name = "label2";
        label2.Padding = new Padding(0, 0, 6, 0);
        label2.Size = new Size(139, 23);
        label2.TabIndex = 4;
        label2.Text = "Customer Name *:";
        label2.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _nameEdit
        //
        _nameEdit.Dock = DockStyle.Fill;
        _nameEdit.Location = new Point(555, 95);
        _nameEdit.Margin = new Padding(0, 6, 0, 6);
        _nameEdit.Name = "_nameEdit";
        _nameEdit.Size = new Size(249, 20);
        _nameEdit.TabIndex = 5;
        //
        // label3
        //
        label3.Dock = DockStyle.Fill;
        label3.Location = new Point(16, 130);
        label3.Margin = new Padding(0, 6, 6, 6);
        label3.Name = "label3";
        label3.Padding = new Padding(0, 0, 6, 0);
        label3.Size = new Size(139, 23);
        label3.TabIndex = 6;
        label3.Text = "Mobile Number *:";
        label3.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _mobileEdit
        //
        _mobileEdit.Dock = DockStyle.Fill;
        _mobileEdit.Location = new Point(161, 130);
        _mobileEdit.Margin = new Padding(0, 6, 16, 6);
        _mobileEdit.Name = "_mobileEdit";
        _mobileEdit.Size = new Size(233, 20);
        _mobileEdit.TabIndex = 7;
        //
        // labelMobile2
        //
        labelMobile2.Dock = DockStyle.Fill;
        labelMobile2.Location = new Point(410, 130);
        labelMobile2.Margin = new Padding(0, 6, 6, 6);
        labelMobile2.Name = "labelMobile2";
        labelMobile2.Padding = new Padding(0, 0, 6, 0);
        labelMobile2.Size = new Size(139, 23);
        labelMobile2.TabIndex = 8;
        labelMobile2.Text = "Mobile 2:";
        labelMobile2.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _mobile2Edit
        //
        _mobile2Edit.Dock = DockStyle.Fill;
        _mobile2Edit.Location = new Point(555, 130);
        _mobile2Edit.Margin = new Padding(0, 6, 0, 6);
        _mobile2Edit.Name = "_mobile2Edit";
        _mobile2Edit.Size = new Size(249, 20);
        _mobile2Edit.TabIndex = 9;
        //
        // labelPhone
        //
        labelPhone.Dock = DockStyle.Fill;
        labelPhone.Location = new Point(16, 165);
        labelPhone.Margin = new Padding(0, 6, 6, 6);
        labelPhone.Name = "labelPhone";
        labelPhone.Padding = new Padding(0, 0, 6, 0);
        labelPhone.Size = new Size(139, 23);
        labelPhone.TabIndex = 10;
        labelPhone.Text = "Phone:";
        labelPhone.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _phoneEdit
        //
        _phoneEdit.Dock = DockStyle.Fill;
        _phoneEdit.Location = new Point(161, 165);
        _phoneEdit.Margin = new Padding(0, 6, 16, 6);
        _phoneEdit.Name = "_phoneEdit";
        _phoneEdit.Size = new Size(233, 20);
        _phoneEdit.TabIndex = 11;
        //
        // labelShopNo
        //
        labelShopNo.Dock = DockStyle.Fill;
        labelShopNo.Location = new Point(410, 165);
        labelShopNo.Margin = new Padding(0, 6, 6, 6);
        labelShopNo.Name = "labelShopNo";
        labelShopNo.Padding = new Padding(0, 0, 6, 0);
        labelShopNo.Size = new Size(139, 23);
        labelShopNo.TabIndex = 12;
        labelShopNo.Text = "Shop No:";
        labelShopNo.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _shopNoEdit
        //
        _shopNoEdit.Dock = DockStyle.Fill;
        _shopNoEdit.Location = new Point(555, 165);
        _shopNoEdit.Margin = new Padding(0, 6, 0, 6);
        _shopNoEdit.Name = "_shopNoEdit";
        _shopNoEdit.Size = new Size(249, 20);
        _shopNoEdit.TabIndex = 13;
        //
        // label4
        //
        label4.Dock = DockStyle.Fill;
        label4.Location = new Point(16, 200);
        label4.Margin = new Padding(0, 6, 6, 6);
        label4.Name = "label4";
        label4.Padding = new Padding(0, 0, 6, 0);
        label4.Size = new Size(139, 23);
        label4.TabIndex = 14;
        label4.Text = "Address *:";
        label4.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _addressEdit
        //
        _addressEdit.Dock = DockStyle.Fill;
        _addressEdit.Location = new Point(161, 200);
        _addressEdit.Margin = new Padding(0, 6, 16, 6);
        _addressEdit.Name = "_addressEdit";
        _addressEdit.Size = new Size(233, 20);
        _addressEdit.TabIndex = 15;
        //
        // label5
        //
        label5.Dock = DockStyle.Fill;
        label5.Location = new Point(410, 200);
        label5.Margin = new Padding(0, 6, 6, 6);
        label5.Name = "label5";
        label5.Padding = new Padding(0, 0, 6, 0);
        label5.Size = new Size(139, 23);
        label5.TabIndex = 16;
        label5.Text = "Email:";
        label5.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _emailEdit
        //
        _emailEdit.Dock = DockStyle.Fill;
        _emailEdit.Location = new Point(555, 200);
        _emailEdit.Margin = new Padding(0, 6, 0, 6);
        _emailEdit.Name = "_emailEdit";
        _emailEdit.Size = new Size(249, 20);
        _emailEdit.TabIndex = 17;
        //
        // _lblSectionAccount
        //
        _lblSectionAccount.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblSectionAccount.Appearance.ForeColor = Color.FromArgb(14, 116, 144);
        _lblSectionAccount.Appearance.Options.UseFont = true;
        _lblSectionAccount.Appearance.Options.UseForeColor = true;
        _customerLayout.SetColumnSpan(_lblSectionAccount, 4);
        _lblSectionAccount.Dock = DockStyle.Top;
        _lblSectionAccount.Location = new Point(16, 239);
        _lblSectionAccount.Margin = new Padding(0, 10, 0, 6);
        _lblSectionAccount.Name = "_lblSectionAccount";
        _lblSectionAccount.Padding = new Padding(0, 4, 0, 2);
        _lblSectionAccount.Size = new Size(112, 21);
        _lblSectionAccount.TabIndex = 18;
        _lblSectionAccount.Text = "ACCOUNT OPENING";
        //
        // label6
        //
        label6.Dock = DockStyle.Fill;
        label6.Location = new Point(16, 272);
        label6.Margin = new Padding(0, 6, 6, 6);
        label6.Name = "label6";
        label6.Padding = new Padding(0, 0, 6, 0);
        label6.Size = new Size(139, 23);
        label6.TabIndex = 19;
        label6.Text = "Opening Balance:";
        label6.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _openingBalanceEdit
        //
        _openingBalanceEdit.Dock = DockStyle.Fill;
        _openingBalanceEdit.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
        _openingBalanceEdit.Location = new Point(161, 272);
        _openingBalanceEdit.Margin = new Padding(0, 6, 16, 6);
        _openingBalanceEdit.Name = "_openingBalanceEdit";
        _openingBalanceEdit.Properties.Mask.EditMask = "n2";
        _openingBalanceEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
        _openingBalanceEdit.Size = new Size(233, 20);
        _openingBalanceEdit.TabIndex = 20;
        //
        // _lblSectionCredit
        //
        _lblSectionCredit.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblSectionCredit.Appearance.ForeColor = Color.FromArgb(14, 116, 144);
        _lblSectionCredit.Appearance.Options.UseFont = true;
        _lblSectionCredit.Appearance.Options.UseForeColor = true;
        _customerLayout.SetColumnSpan(_lblSectionCredit, 4);
        _lblSectionCredit.Dock = DockStyle.Top;
        _lblSectionCredit.Location = new Point(16, 311);
        _lblSectionCredit.Margin = new Padding(0, 10, 0, 6);
        _lblSectionCredit.Name = "_lblSectionCredit";
        _lblSectionCredit.Padding = new Padding(0, 4, 0, 2);
        _lblSectionCredit.Size = new Size(167, 21);
        _lblSectionCredit.TabIndex = 21;
        _lblSectionCredit.Text = "CREDIT / ACCOUNT SETTINGS";
        //
        // label7
        //
        label7.Dock = DockStyle.Fill;
        label7.Location = new Point(16, 341);
        label7.Margin = new Padding(0, 3, 6, 4);
        label7.Name = "label7";
        label7.Padding = new Padding(0, 5, 6, 0);
        label7.Size = new Size(139, 56);
        label7.TabIndex = 22;
        label7.Text = "Credit Limit:";
        //
        // pnlCreditLimit
        //
        pnlCreditLimit.AutoSize = true;
        pnlCreditLimit.ColumnCount = 1;
        pnlCreditLimit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pnlCreditLimit.Controls.Add(_creditLimitEdit, 0, 0);
        pnlCreditLimit.Controls.Add(labelCreditLimitHelp, 0, 1);
        pnlCreditLimit.Dock = DockStyle.Top;
        pnlCreditLimit.Location = new Point(161, 341);
        pnlCreditLimit.Margin = new Padding(0, 3, 16, 6);
        pnlCreditLimit.Name = "pnlCreditLimit";
        pnlCreditLimit.RowCount = 2;
        pnlCreditLimit.RowStyles.Add(new RowStyle());
        pnlCreditLimit.RowStyles.Add(new RowStyle());
        pnlCreditLimit.Size = new Size(233, 54);
        pnlCreditLimit.TabIndex = 23;
        //
        // _creditLimitEdit
        //
        _creditLimitEdit.Dock = DockStyle.Top;
        _creditLimitEdit.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
        _creditLimitEdit.Location = new Point(0, 0);
        _creditLimitEdit.Margin = new Padding(0, 0, 0, 5);
        _creditLimitEdit.Name = "_creditLimitEdit";
        _creditLimitEdit.Properties.Mask.EditMask = "n2";
        _creditLimitEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
        _creditLimitEdit.Size = new Size(233, 20);
        _creditLimitEdit.TabIndex = 0;
        //
        // labelCreditLimitHelp
        //
        labelCreditLimitHelp.AutoSize = true;
        labelCreditLimitHelp.Dock = DockStyle.Top;
        labelCreditLimitHelp.Font = new Font("Segoe UI", 8.25F, FontStyle.Italic);
        labelCreditLimitHelp.ForeColor = Color.DimGray;
        labelCreditLimitHelp.Location = new Point(0, 29);
        labelCreditLimitHelp.Margin = new Padding(0, 4, 0, 8);
        labelCreditLimitHelp.Name = "labelCreditLimitHelp";
        labelCreditLimitHelp.Padding = new Padding(0, 2, 0, 2);
        labelCreditLimitHelp.Size = new Size(233, 17);
        labelCreditLimitHelp.TabIndex = 1;
        labelCreditLimitHelp.Text = "0.00 = Zero Credit Limit (no credit extended).";
        //
        // labelCreditAllowed
        //
        labelCreditAllowed.Dock = DockStyle.Fill;
        labelCreditAllowed.Location = new Point(410, 341);
        labelCreditAllowed.Margin = new Padding(0, 3, 6, 4);
        labelCreditAllowed.Name = "labelCreditAllowed";
        labelCreditAllowed.Padding = new Padding(0, 5, 6, 0);
        labelCreditAllowed.Size = new Size(139, 56);
        labelCreditAllowed.TabIndex = 24;
        labelCreditAllowed.Text = "Credit Facility:";
        //
        // _isCreditAllowedCheck
        //
        _isCreditAllowedCheck.Dock = DockStyle.Top;
        _isCreditAllowedCheck.Location = new Point(555, 341);
        _isCreditAllowedCheck.Margin = new Padding(0, 3, 0, 2);
        _isCreditAllowedCheck.Name = "_isCreditAllowedCheck";
        _isCreditAllowedCheck.Properties.Caption = "Allow on-account purchases";
        _isCreditAllowedCheck.Size = new Size(249, 20);
        _isCreditAllowedCheck.TabIndex = 25;
        //
        // labelDefault
        //
        labelDefault.Dock = DockStyle.Fill;
        labelDefault.Location = new Point(16, 405);
        labelDefault.Margin = new Padding(0, 4, 6, 4);
        labelDefault.Name = "labelDefault";
        labelDefault.Padding = new Padding(0, 0, 6, 0);
        labelDefault.Size = new Size(139, 23);
        labelDefault.TabIndex = 26;
        labelDefault.Text = "Default Customer:";
        labelDefault.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _isDefaultCheck
        //
        _customerLayout.SetColumnSpan(_isDefaultCheck, 3);
        _isDefaultCheck.Dock = DockStyle.Top;
        _isDefaultCheck.Location = new Point(161, 405);
        _isDefaultCheck.Margin = new Padding(0, 4, 0, 4);
        _isDefaultCheck.Name = "_isDefaultCheck";
        _isDefaultCheck.Properties.Caption = "Set as default customer for new POS orders";
        _isDefaultCheck.Size = new Size(643, 20);
        _isDefaultCheck.TabIndex = 27;
        //
        // _lblSectionNotes
        //
        _lblSectionNotes.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblSectionNotes.Appearance.ForeColor = Color.FromArgb(14, 116, 144);
        _lblSectionNotes.Appearance.Options.UseFont = true;
        _lblSectionNotes.Appearance.Options.UseForeColor = true;
        _customerLayout.SetColumnSpan(_lblSectionNotes, 4);
        _lblSectionNotes.Dock = DockStyle.Top;
        _lblSectionNotes.Location = new Point(16, 442);
        _lblSectionNotes.Margin = new Padding(0, 10, 0, 6);
        _lblSectionNotes.Name = "_lblSectionNotes";
        _lblSectionNotes.Padding = new Padding(0, 4, 0, 2);
        _lblSectionNotes.Size = new Size(38, 21);
        _lblSectionNotes.TabIndex = 28;
        _lblSectionNotes.Text = "NOTES";
        //
        // label8
        //
        label8.Dock = DockStyle.Fill;
        label8.Location = new Point(16, 474);
        label8.Margin = new Padding(0, 5, 6, 4);
        label8.Name = "label8";
        label8.Padding = new Padding(0, 4, 6, 0);
        label8.Size = new Size(139, 96);
        label8.TabIndex = 29;
        label8.Text = "Notes:";
        //
        // _notesEdit
        //
        _customerLayout.SetColumnSpan(_notesEdit, 3);
        _notesEdit.Dock = DockStyle.Fill;
        _notesEdit.Location = new Point(161, 474);
        _notesEdit.Margin = new Padding(0, 5, 0, 4);
        _notesEdit.MinimumSize = new Size(0, 65);
        _notesEdit.Name = "_notesEdit";
        _notesEdit.Size = new Size(643, 96);
        _notesEdit.TabIndex = 30;
        //
        // CustomerEditForm
        //
        ClientSize = new Size(820, 620);
        Controls.Add(_customerLayout);
        MinimumSize = new Size(760, 540);
        Name = "CustomerEditForm";
        Controls.SetChildIndex(_customerLayout, 0);
        _customerLayout.ResumeLayout(false);
        _customerLayout.PerformLayout();
        _pnlHeader.ResumeLayout(false);
        _pnlHeader.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_codeEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_mobileEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_mobile2Edit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_phoneEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_shopNoEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_addressEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_emailEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_openingBalanceEdit.Properties).EndInit();
        pnlCreditLimit.ResumeLayout(false);
        pnlCreditLimit.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_creditLimitEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_isCreditAllowedCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_isDefaultCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_notesEdit.Properties).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    private TableLayoutPanel _customerLayout;
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
    private DevExpress.XtraEditors.MemoEdit _notesEdit;

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
    private TableLayoutPanel pnlCreditLimit;
}
