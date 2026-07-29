using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Catalog.Brands;

/// <summary>Create/edit dialog for a Brand - just a name.</summary>
public sealed class BrandEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _nameEdit = new();

    /// <summary>Builds the dialog.</summary>
    public BrandEditForm(string title, string? name = null) : base(title)
    {
        _nameEdit.Text = name ?? string.Empty;

        AddField("Name:", _nameEdit);
    }

    /// <summary>The entered brand name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
