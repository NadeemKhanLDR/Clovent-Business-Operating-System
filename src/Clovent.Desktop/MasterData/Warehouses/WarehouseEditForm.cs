using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData.Warehouses;

/// <summary>
/// Create/edit dialog for a Warehouse - name and, for new warehouses only,
/// a code (immutable after creation, so the code field is disabled when
/// editing an existing warehouse).
/// </summary>
public sealed class WarehouseEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _nameEdit = new();
    private readonly TextEdit _codeEdit = new();

    /// <summary>Builds the dialog. Pass <paramref name="code"/> when editing so the (disabled) field still shows it.</summary>
    public WarehouseEditForm(string title, string? name = null, string? code = null, bool isNew = true) : base(title)
    {
        _nameEdit.Text = name ?? string.Empty;
        _codeEdit.Text = code ?? string.Empty;
        _codeEdit.Enabled = isNew;

        AddField("Name:", _nameEdit);
        AddField("Code:", _codeEdit);
    }

    /// <summary>The entered warehouse name.</summary>
    public string WarehouseNameValue => _nameEdit.Text.Trim();

    /// <summary>The entered warehouse code (only meaningful when creating).</summary>
    public string CodeValue => _codeEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        if (_codeEdit.Enabled && string.IsNullOrWhiteSpace(_codeEdit.Text))
        {
            error = "Code is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
