using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.DiningAreas;

/// <summary>Create/edit dialog for a Dining Area - name only.</summary>
public sealed class DiningAreaEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _nameEdit = new();

    /// <summary>Builds the dialog.</summary>
    public DiningAreaEditForm(string title, string? name = null) : base(title)
    {
        _nameEdit.Text = name ?? string.Empty;

        AddField("Name:", _nameEdit);
    }

    /// <summary>The entered dining area name.</summary>
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
