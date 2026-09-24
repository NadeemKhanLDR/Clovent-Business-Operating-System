using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Restaurant.Customers;

/// <summary>
/// Create/edit dialog for a Customer - handles all customer properties.
/// Visual Studio Designer compatible.
/// </summary>
public sealed partial class CustomerEditForm : MasterDataEditFormBase
{
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CustomerEditForm() : base("Edit Customer")
    {
        InitializeComponent();
        }

    /// <summary>Builds the dialog.</summary>
    public CustomerEditForm(
        string title,
        string? code = null,
        string? name = null,
        string? mobileNumber = null,
        string? address = null,
        string? email = null,
        decimal openingBalance = 0m,
        decimal creditLimit = 0m,
        string? notes = null,
        bool isNew = true,
        string? shopNo = null,
        string? mobile2 = null,
        string? phone = null,
        bool isDefault = false) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _codeEdit.Text = code ?? string.Empty;
        _nameEdit.Text = name ?? string.Empty;
        _mobileEdit.Text = mobileNumber ?? string.Empty;
        _mobile2Edit.Text = mobile2 ?? string.Empty;
        _phoneEdit.Text = phone ?? string.Empty;
        _shopNoEdit.Text = shopNo ?? string.Empty;
        _addressEdit.Text = address ?? string.Empty;
        _emailEdit.Text = email ?? string.Empty;
        _openingBalanceEdit.Value = openingBalance;
        _creditLimitEdit.Value = creditLimit;
        _isDefaultCheck.Checked = isDefault;
        _notesEdit.Text = notes ?? string.Empty;

        _codeEdit.Properties.ReadOnly = true;
        _openingBalanceEdit.Enabled = isNew;

        _openingBalanceEdit.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        _openingBalanceEdit.Properties.Mask.EditMask = "F" + Clovent.Desktop.Forms.Base.CurrencyDisplay.DecimalPlaces;
        _openingBalanceEdit.Properties.Mask.UseMaskAsDisplayFormat = true;

        _creditLimitEdit.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        _creditLimitEdit.Properties.Mask.EditMask = "F" + Clovent.Desktop.Forms.Base.CurrencyDisplay.DecimalPlaces;
        _creditLimitEdit.Properties.Mask.UseMaskAsDisplayFormat = true;

        SetFixedRowHeight(_notesEdit, 80);
    }

    /// <summary>The entered customer code.</summary>
    public string CodeValue => _codeEdit.Text.Trim();

    /// <summary>The entered customer name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <summary>The entered mobile number.</summary>
    public string MobileValue => _mobileEdit.Text.Trim();

    /// <summary>The entered secondary mobile number (optional).</summary>
    public string? Mobile2Value => string.IsNullOrWhiteSpace(_mobile2Edit.Text) ? null : _mobile2Edit.Text.Trim();

    /// <summary>The entered secondary phone number (optional).</summary>
    public string? PhoneValue => string.IsNullOrWhiteSpace(_phoneEdit.Text) ? null : _phoneEdit.Text.Trim();

    /// <summary>The entered shop number (optional).</summary>
    public string? ShopNoValue => string.IsNullOrWhiteSpace(_shopNoEdit.Text) ? null : _shopNoEdit.Text.Trim();

    /// <summary>The entered address.</summary>
    public string AddressValue => _addressEdit.Text.Trim();

    /// <summary>The entered email address (optional).</summary>
    public string? EmailValue => string.IsNullOrWhiteSpace(_emailEdit.Text) ? null : _emailEdit.Text.Trim();

    /// <summary>The entered opening balance.</summary>
    public decimal OpeningBalanceValue => _openingBalanceEdit.Value;

    /// <summary>The entered credit limit.</summary>
    public decimal CreditLimitValue => _creditLimitEdit.Value;

    /// <summary>The entered notes (optional).</summary>
    public string? NotesValue => string.IsNullOrWhiteSpace(_notesEdit.Text) ? null : _notesEdit.Text.Trim();

    /// <summary>Whether this customer is designated as the POS default customer.</summary>
    public bool IsDefaultValue => _isDefaultCheck.Checked;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_codeEdit.Text))
        {
            error = "Customer Code is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Customer Name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_mobileEdit.Text))
        {
            error = "Mobile Number is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_addressEdit.Text))
        {
            error = "Address is required.";
            return false;
        }

        if (_openingBalanceEdit.Value < 0)
        {
            error = "Opening Balance cannot be negative.";
            return false;
        }

        if (_creditLimitEdit.Value < 0)
        {
            error = "Credit Limit cannot be negative.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
