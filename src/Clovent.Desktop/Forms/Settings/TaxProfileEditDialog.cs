using System;
using System.Windows.Forms;
using Clovent.Catalog.TaxProfiles;
using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace Clovent.Desktop.Forms.Settings;

/// <summary>
/// Edit dialog for creating or updating a statutory Sales Tax Profile.
/// Enforces statutory rules, rate bounds, and authority validation.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class TaxProfileEditDialog : MasterDataEditFormBase
{
    private readonly TextEdit _codeEdit = new();
    private readonly TextEdit _displayNameEdit = new();
    private readonly ComboBoxEdit _authorityCombo = new();
    private readonly ComboBoxEdit _jurisdictionCombo = new();
    private readonly ComboBoxEdit _taxClassificationCombo = new();
    private readonly ComboBoxEdit _itemClassificationCombo = new();
    private readonly SpinEdit _rateEdit = new();
    private readonly ComboBoxEdit _pricingModeCombo = new();
    private readonly TextEdit _refDocEdit = new();
    private readonly TextEdit _notesEdit = new();
    private readonly bool _isNew;

    /// <summary>Gets the normalized uppercase statutory tax profile code.</summary>
    public string ProfileCode => _codeEdit.Text.Trim().ToUpperInvariant();

    /// <summary>Gets the tax profile display name shown on customer invoices.</summary>
    public string InvoiceDisplayName => _displayNameEdit.Text.Trim();

    /// <summary>Gets the statutory revenue authority code (e.g., PRA, SRB, FBR).</summary>
    public string Authority => _authorityCombo.Text.Trim().ToUpperInvariant();

    /// <summary>Gets the provincial or federal tax jurisdiction.</summary>
    public string Jurisdiction => _jurisdictionCombo.Text.Trim();

    /// <summary>Gets the selected statutory taxability classification.</summary>
    public TaxClassification TaxClassification => Enum.Parse<TaxClassification>(_taxClassificationCombo.Text);

    /// <summary>Gets the selected item classification (Goods or Services).</summary>
    public ItemTaxClassification ItemClassification => Enum.Parse<ItemTaxClassification>(_itemClassificationCombo.Text);

    /// <summary>Gets the statutory tax rate percentage.</summary>
    public decimal RatePercentage => _rateEdit.Value;

    /// <summary>Gets the tax pricing mode (Exclusive or Inclusive).</summary>
    public TaxPricingMode PricingMode => Enum.Parse<TaxPricingMode>(_pricingModeCombo.Text);

    /// <summary>Gets the optional statutory gazette or SRO reference document identifier.</summary>
    public string? ReferenceDocument => string.IsNullOrWhiteSpace(_refDocEdit.Text) ? null : _refDocEdit.Text.Trim();

    /// <summary>Gets the optional internal audit notes for the tax profile.</summary>
    public string? Notes => string.IsNullOrWhiteSpace(_notesEdit.Text) ? null : _notesEdit.Text.Trim();

    /// <summary>Designer-only constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public TaxProfileEditDialog() : this(true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaxProfileEditDialog"/> class for creating or editing a tax profile.
    /// </summary>
    public TaxProfileEditDialog(
        bool isNew,
        string? code = null,
        string? displayName = null,
        string? authority = "PRA",
        string? jurisdiction = "Punjab",
        TaxClassification taxClassification = TaxClassification.Taxable,
        ItemTaxClassification itemClassification = ItemTaxClassification.Services,
        decimal ratePercentage = 16.00m,
        TaxPricingMode pricingMode = TaxPricingMode.Exclusive,
        string? referenceDocument = null,
        string? notes = null) : base(isNew ? "New Tax Profile" : "Edit Tax Profile")
    {
        _isNew = isNew;

        _authorityCombo.Properties.Items.AddRange(new[] { "PRA", "SRB", "KPRA", "BRA", "ICTRA", "FBR" });
        _authorityCombo.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;

        _jurisdictionCombo.Properties.Items.AddRange(new[] { "Punjab", "Sindh", "Khyber Pakhtunkhwa", "Balochistan", "Islamabad", "Federal" });
        _jurisdictionCombo.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;

        _taxClassificationCombo.Properties.Items.AddRange(Enum.GetNames<TaxClassification>());
        _taxClassificationCombo.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;

        _itemClassificationCombo.Properties.Items.AddRange(Enum.GetNames<ItemTaxClassification>());
        _itemClassificationCombo.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;

        _pricingModeCombo.Properties.Items.AddRange(Enum.GetNames<TaxPricingMode>());
        _pricingModeCombo.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;

        _rateEdit.Properties.MinValue = 0m;
        _rateEdit.Properties.MaxValue = 100m;
        _rateEdit.Properties.Increment = 1m;
        _rateEdit.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        _rateEdit.Properties.DisplayFormat.FormatString = "n2";

        _codeEdit.Text = code ?? string.Empty;
        if (!_isNew)
        {
            _codeEdit.ReadOnly = true;
        }

        _displayNameEdit.Text = displayName ?? string.Empty;
        _authorityCombo.SelectedItem = authority ?? "PRA";
        _jurisdictionCombo.SelectedItem = jurisdiction ?? "Punjab";
        _taxClassificationCombo.SelectedItem = taxClassification.ToString();
        _itemClassificationCombo.SelectedItem = itemClassification.ToString();
        _rateEdit.Value = ratePercentage;
        _pricingModeCombo.SelectedItem = pricingMode.ToString();
        _refDocEdit.Text = referenceDocument ?? string.Empty;
        _notesEdit.Text = notes ?? string.Empty;

        _taxClassificationCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_taxClassificationCombo.Text != "Taxable")
            {
                _rateEdit.Value = 0m;
                _rateEdit.Enabled = false;
            }
            else
            {
                _rateEdit.Enabled = true;
            }
        };

        AddField("Statutory Code:", _codeEdit);
        AddField("Invoice Display Name:", _displayNameEdit);
        AddField("Tax Authority:", _authorityCombo);
        AddField("Jurisdiction:", _jurisdictionCombo);
        AddField("Taxability:", _taxClassificationCombo);
        AddField("Item Classification:", _itemClassificationCombo);
        AddField("Rate (%):", _rateEdit);
        AddField("Pricing Mode:", _pricingModeCombo);
        AddField("Gazette / Ref Document:", _refDocEdit);
        AddField("Audit Notes:", _notesEdit);
    }

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_codeEdit.Text))
        {
            error = "Tax profile code cannot be empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_displayNameEdit.Text))
        {
            error = "Invoice display name cannot be empty.";
            return false;
        }

        if (_rateEdit.Value < 0m || _rateEdit.Value > 100m)
        {
            error = "Tax rate must be between 0 and 100 percent.";
            return false;
        }

        if (_taxClassificationCombo.Text != "Taxable" && _rateEdit.Value != 0m)
        {
            error = "Exempt, zero-rated, and out-of-scope tax profiles must have a 0% tax rate.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
