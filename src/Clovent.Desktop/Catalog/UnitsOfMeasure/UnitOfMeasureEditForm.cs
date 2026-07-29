using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Catalog.UnitsOfMeasure;

/// <summary>
/// Create/edit dialog for a Unit of Measure - code and name. The code is
/// immutable after creation (mirrors <c>WarehouseEditForm</c>'s identical
/// "code fixed at creation" reasoning), so the field is disabled when
/// editing an existing unit.
/// </summary>
public sealed class UnitOfMeasureEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _codeEdit = new();
    private readonly TextEdit _nameEdit = new();

    /// <summary>Builds the dialog. Pass <paramref name="code"/> when editing so the (disabled) field still shows it.</summary>
    public UnitOfMeasureEditForm(string title, string? code = null, string? name = null, bool isNew = true) : base(title)
    {
        _codeEdit.Text = code ?? string.Empty;
        _codeEdit.Enabled = isNew;
        _nameEdit.Text = name ?? string.Empty;

        AddField("Code (e.g. KG):", _codeEdit);
        AddField("Name:", _nameEdit);
    }

    /// <summary>The entered code (only meaningful when creating).</summary>
    public string CodeValue => _codeEdit.Text.Trim();

    /// <summary>The entered display name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (_codeEdit.Enabled && string.IsNullOrWhiteSpace(_codeEdit.Text))
        {
            error = "Code is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
