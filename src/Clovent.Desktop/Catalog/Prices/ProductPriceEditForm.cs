using Clovent.Catalog.Prices;
using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Catalog.Prices;

/// <summary>
/// Create/edit dialog for a Product Price. Price type and currency are
/// fixed at creation (<see cref="ProductPrice"/> exposes no method to
/// change either), so those two fields are disabled when editing an
/// existing price - only the amount can change.
/// </summary>
public sealed class ProductPriceEditForm : MasterDataEditFormBase
{
    private readonly ComboBoxEdit _priceTypeCombo = new();
    private readonly ComboBoxEdit _currencyCombo = new();
    private readonly SpinEdit _amountEdit = new() { Properties = { MinValue = 0, MaxValue = 1_000_000, Increment = 0.01m } };
    private readonly Dictionary<string, Guid?> _currenciesByDisplay;

    /// <summary>Builds the dialog. <paramref name="currencyOptions"/> must be non-empty when creating.</summary>
    public ProductPriceEditForm(
        string title,
        IReadOnlyList<(Guid Id, string Display)> currencyOptions,
        PriceType priceType = PriceType.Selling,
        Guid? currencyId = null,
        decimal amount = 0,
        bool isNew = true) : base(title)
    {
        _priceTypeCombo.Properties.Items.AddRange([nameof(PriceType.Cost), nameof(PriceType.Selling)]);
        _priceTypeCombo.SelectedItem = priceType.ToString();
        _priceTypeCombo.Enabled = isNew;

        _currenciesByDisplay = ComboBoxBinder.Bind(_currencyCombo, currencyOptions, includeEmpty: false);
        ComboBoxBinder.SelectById(_currencyCombo, _currenciesByDisplay, currencyId);
        _currencyCombo.Enabled = isNew;

        _amountEdit.Value = amount;

        AddField("Price Type:", _priceTypeCombo);
        AddField("Currency:", _currencyCombo);
        AddField("Amount:", _amountEdit);
    }

    /// <summary>The selected price type.</summary>
    public PriceType PriceType => Enum.Parse<PriceType>((string)_priceTypeCombo.SelectedItem);

    /// <summary>The selected currency.</summary>
    public Guid? CurrencyId => ComboBoxBinder.GetSelectedId(_currencyCombo, _currenciesByDisplay);

    /// <summary>The entered amount.</summary>
    public decimal Amount => _amountEdit.Value;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (_currencyCombo.Enabled && CurrencyId is null)
        {
            error = "A currency is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
