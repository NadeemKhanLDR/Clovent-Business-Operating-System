using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.MasterData.Currencies;

/// <summary>
/// Create dialog for a Currency catalog entry. There is no edit dialog -
/// <c>Currency</c> exposes no update method beyond Activate/Deactivate (see
/// <c>Clovent.MasterData.Currencies.Currency</c>'s doc comment), so
/// <see cref="MasterDataListView{TDto}.OnEdit"/> is deliberately left unset
/// for the Currency screen.
/// </summary>
public sealed partial class CurrencyCreateForm : MasterDataEditFormBase
{
    /// <summary>Builds the dialog.</summary>
    public CurrencyCreateForm() : base("New Currency")
    {
        InitializeComponent();
        }

    /// <summary>The entered ISO 4217 code.</summary>
    public string Code => _codeEdit.Text.Trim();

    /// <summary>The entered display name.</summary>
    public string CurrencyNameValue => _nameEdit.Text.Trim();

    /// <summary>The entered symbol.</summary>
    public string Symbol => _symbolEdit.Text.Trim();

    /// <summary>The entered number of decimal places.</summary>
    public int DecimalPlaces => (int)_decimalPlacesEdit.Value;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_codeEdit.Text))
        {
            error = "Code is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_symbolEdit.Text))
        {
            error = "Symbol is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
