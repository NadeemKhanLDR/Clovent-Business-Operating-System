using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.Barcodes;

/// <summary>
/// Create dialog for a Barcode. There is no edit dialog - <c>Barcode</c>
/// exposes no update method for its scanned value beyond Mark/UnmarkAsPrimary
/// and Activate/Deactivate (see <c>Clovent.Catalog.Barcodes.Barcode</c>'s doc
/// comment), so <see cref="MasterDataListView{TDto}.OnEdit"/> is
/// deliberately left unset for the Barcode screen, mirroring
/// <c>CurrencyCreateForm</c>'s identical "no edit" reasoning. Control tree
/// (fields, <c>AddField</c> calls) lives in
/// <c>BarcodeCreateForm.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class BarcodeCreateForm : MasterDataEditFormBase
{
    /// <summary>Builds the dialog.</summary>
    public BarcodeCreateForm() : base("New Barcode")
    {
        InitializeComponent();
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
