using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Catalog.Barcodes;

/// <summary>
/// Create dialog for a Barcode. There is no edit dialog - <c>Barcode</c>
/// exposes no update method for its scanned value beyond Mark/UnmarkAsPrimary
/// and Activate/Deactivate (see <c>Clovent.Catalog.Barcodes.Barcode</c>'s doc
/// comment), so <see cref="MasterDataListView{TDto}.OnEdit"/> is
/// deliberately left unset for the Barcode screen, mirroring
/// <c>CurrencyCreateForm</c>'s identical "no edit" reasoning.
/// </summary>
public sealed class BarcodeCreateForm : MasterDataEditFormBase
{
    private readonly TextEdit _valueEdit = new();
    private readonly CheckEdit _isPrimaryEdit = new() { Text = "Set as primary barcode" };

    /// <summary>Builds the dialog.</summary>
    public BarcodeCreateForm() : base("New Barcode")
    {
        AddField("Value (8-14 digits):", _valueEdit);
        AddField(string.Empty, _isPrimaryEdit);
    }

    /// <summary>The entered barcode value.</summary>
    public string Value => _valueEdit.Text.Trim();

    /// <summary>Whether this should be marked the variant's primary barcode.</summary>
    public bool IsPrimary => _isPrimaryEdit.Checked;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_valueEdit.Text))
        {
            error = "Value is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
