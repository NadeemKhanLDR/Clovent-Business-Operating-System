using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData.Currencies;

/// <summary>
/// Create dialog for a Currency catalog entry. There is no edit dialog -
/// <c>Currency</c> exposes no update method beyond Activate/Deactivate (see
/// <c>Clovent.MasterData.Currencies.Currency</c>'s doc comment), so
/// <see cref="MasterDataListView{TDto}.OnEdit"/> is deliberately left unset
/// for the Currency screen.
/// </summary>
public sealed class CurrencyCreateForm : MasterDataEditFormBase
{
    private readonly TextEdit _codeEdit = new();
    private readonly TextEdit _nameEdit = new();
    private readonly TextEdit _symbolEdit = new();
    private readonly SpinEdit _decimalPlacesEdit = new() { Properties = { MinValue = 0, MaxValue = 4 }, Value = 2 };

    /// <summary>Builds the dialog.</summary>
    public CurrencyCreateForm() : base("New Currency")
    {
        AddField("Code (e.g. USD):", _codeEdit);
        AddField("Name:", _nameEdit);
        AddField("Symbol:", _symbolEdit);
        AddField("Decimal Places:", _decimalPlacesEdit);
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
