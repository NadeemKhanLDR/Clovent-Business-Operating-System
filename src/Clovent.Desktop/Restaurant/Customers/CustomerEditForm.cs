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
        bool isNew = true) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _codeEdit.Text = code ?? string.Empty;
        _nameEdit.Text = name ?? string.Empty;
        _mobileEdit.Text = mobileNumber ?? string.Empty;
        _addressEdit.Text = address ?? string.Empty;
        _emailEdit.Text = email ?? string.Empty;
        _openingBalanceEdit.Value = openingBalance;
        _creditLimitEdit.Value = creditLimit;
        _notesEdit.Text = notes ?? string.Empty;

        _codeEdit.Enabled = isNew;
        _openingBalanceEdit.Enabled = isNew;
    }

    /// <summary>The entered customer code.</summary>
    public string CodeValue => _codeEdit.Text.Trim();

    /// <summary>The entered customer name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <summary>The entered mobile number.</summary>
    public string MobileValue => _mobileEdit.Text.Trim();

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
