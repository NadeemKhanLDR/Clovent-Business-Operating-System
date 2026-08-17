using Clovent.Catalog.Prices;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.Prices;

/// <summary>
/// Create/edit dialog for a Product Price. Price type and currency are
/// fixed at creation (<see cref="ProductPrice"/> exposes no method to
/// change either), so those two fields are disabled when editing an
/// existing price - only the amount can change. Control tree (fields,
/// <c>AddField</c> calls) lives in <c>ProductPriceEditForm.Designer.cs</c>;
/// this file holds behavior only.
/// </summary>
public sealed partial class ProductPriceEditForm : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid?> _currenciesByDisplay;

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ProductPriceEditForm() : base("Edit Price")
    {
        _currenciesByDisplay = null!;

        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog. <paramref name="title"/> is the dialog's caption.
    /// <paramref name="currencyOptions"/> must be non-empty when creating.
    /// <paramref name="priceType"/>, <paramref name="currencyId"/>, and
    /// <paramref name="amount"/> pre-populate the fields; both price type and
    /// currency are disabled unless <paramref name="isNew"/> is
    /// <see langword="true"/>, since <see cref="ProductPrice"/> exposes no
    /// method to change either after creation.
    /// </summary>
    public ProductPriceEditForm(
        string title,
        IReadOnlyList<(Guid Id, string Display)> currencyOptions,
        PriceType priceType = PriceType.Selling,
        Guid? currencyId = null,
        decimal amount = 0,
        bool isNew = true) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _currenciesByDisplay = null!;
            return;
        }

        _priceTypeCombo.SelectedItem = priceType.ToString();
        _priceTypeCombo.Enabled = isNew;

        _currenciesByDisplay = ComboBoxBinder.Bind(_currencyCombo, currencyOptions, includeEmpty: false);
        ComboBoxBinder.SelectById(_currencyCombo, _currenciesByDisplay, currencyId);
        _currencyCombo.Enabled = isNew;

        _amountEdit.Value = amount;
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
