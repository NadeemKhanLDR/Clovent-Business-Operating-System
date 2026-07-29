using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData.Departments;

/// <summary>Create/edit dialog for a Department - just a name.</summary>
public sealed class DepartmentEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _nameEdit = new();

    /// <summary>Builds the dialog, pre-filled when editing an existing department.</summary>
    public DepartmentEditForm(string title, string? name = null) : base(title)
    {
        _nameEdit.Text = name ?? string.Empty;
        AddField("Name:", _nameEdit);
    }

    /// <summary>The entered department name.</summary>
    public string DepartmentNameValue => _nameEdit.Text.Trim();

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
